# #40 — Write EventPublished to outbox on publication

## Summary

When an event is published, write an `EventPublished` integration event to the
`events.outbox_messages` table in the same `SaveChanges` transaction as the
state change. The outbox row becomes the durable signal that other modules will
eventually consume.

## Scope

- In scope:
  - `EventPublishedIntegrationEvent` record in `Contracts/`
  - Non-generic `IAggregateRoot` interface in `BuildingBlocks.Domain` so
    `EventsDbContext` can enumerate domain events across all tracked aggregates
    without knowing each concrete type
  - `EventsDbContext.SaveChangesAsync` override that converts raised domain
    events to outbox messages before flushing to the database
  - Unit test asserting that publishing an event produces exactly one outbox
    message with the correct `Type` and a payload containing the expected
    `EventId` and `ManifestId`
- Out of scope:
  - Outbox dispatcher / background service
  - Consuming module handlers
  - Any new migration (the outbox table already exists)

## Acceptance Criteria

- [ ] `EventPublishedIntegrationEvent` exists in
      `VenuePass.Modules.Events.Contracts` and implements `IIntegrationEvent`
- [ ] `IAggregateRoot` is declared in `VenuePass.BuildingBlocks.Domain` and
      `AggregateRoot<TId>` implements it
- [ ] `EventsDbContext.SaveChangesAsync` iterates tracked `IAggregateRoot`
      entries, creates one `OutboxMessage` per domain event, clears domain
      events, then calls `base.SaveChangesAsync`
- [ ] The `Type` stored in the outbox row is the full CLR type name of the
      integration event (`typeof(EventPublishedIntegrationEvent).FullName`)
- [ ] The `Payload` is a JSON-serialised `EventPublishedIntegrationEvent`
      using `System.Text.Json`
- [ ] A unit test (in `VenuePass.Modules.Events.Tests`) verifies that
      after `PublishEventHandler.Handle(...)`, the tracked
      `OutboxMessages` set contains exactly one message with the correct
      type and deserializable payload
- [ ] `dotnet build` and `dotnet test` pass

## Vertical Slices

- [ ] Add `IAggregateRoot` to `BuildingBlocks.Domain`; implement on
      `AggregateRoot<TId>`
- [ ] Add `EventPublishedIntegrationEvent` to `Contracts/`
- [ ] Override `SaveChangesAsync` in `EventsDbContext`
- [ ] Add unit test

## Design Notes

### IAggregateRoot

EF Core's `ChangeTracker.Entries()` returns untyped `EntityEntry` objects.
To collect domain events without enumerating each concrete type, introduce a
non-generic interface:

```csharp
// VenuePass.BuildingBlocks/Domain/IAggregateRoot.cs
public interface IAggregateRoot
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

`AggregateRoot<TId>` already has both members — adding `: IAggregateRoot`
requires no other change.

### EventPublishedIntegrationEvent

`IIntegrationEvent` declares `Guid EventId` (the integration event's own
identity). The domain payload also carries the venue-event's ID, which would
collide with that name. Use `VenueEventId` for the payload field:

```csharp
// VenuePass.Modules.Events/Contracts/EventPublishedIntegrationEvent.cs
public sealed record EventPublishedIntegrationEvent(
    Guid EventId,           // IIntegrationEvent.EventId — this event's identity
    Guid VenueEventId,      // the published Event's domain ID
    Guid ManifestId,
    DateTimeOffset OccurredOn) : IIntegrationEvent;
```

`EventId` is populated from `domainEvent.DomainEventId` (already a Guid v7).

### SaveChangesAsync override

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var aggregates = ChangeTracker
        .Entries<IAggregateRoot>()
        .Where(e => e.Entity.DomainEvents.Count > 0)
        .Select(e => e.Entity)
        .ToList();

    foreach (var aggregate in aggregates)
    {
        foreach (var domainEvent in aggregate.DomainEvents)
        {
            var integrationEvent = MapToIntegrationEvent(domainEvent);
            if (integrationEvent is null) continue;

            var payload = JsonSerializer.Serialize(integrationEvent,
                integrationEvent.GetType());

            OutboxMessages.Add(OutboxMessage.Create(
                domainEvent.OccurredOn,
                integrationEvent.GetType().FullName!,
                payload));
        }

        aggregate.ClearDomainEvents();
    }

    return await base.SaveChangesAsync(ct);
}
```

`MapToIntegrationEvent` is a private method on `EventsDbContext` that
switches on the concrete domain event type. Only domain events for which
an integration event exists produce an outbox row — unknown events are
silently skipped:

```csharp
private static IIntegrationEvent? MapToIntegrationEvent(DomainEvent domainEvent)
    => domainEvent switch
    {
        EventPublishedDomainEvent e => new EventPublishedIntegrationEvent(
            e.DomainEventId,
            e.EventId.Value,
            e.ManifestId.Value,
            e.OccurredOn),
        _ => null
    };
```

### Unit test approach

The test uses an in-memory or SQLite `EventsDbContext`, calls
`PublishEventHandler.Handle(...)`, and then asserts on
`db.OutboxMessages.Local`:

```csharp
var message = Assert.Single(db.OutboxMessages.Local);
Assert.Equal(typeof(EventPublishedIntegrationEvent).FullName, message.Type);

var payload = JsonSerializer.Deserialize<EventPublishedIntegrationEvent>(message.Payload);
Assert.Equal((Guid)publishedEvent.Id, payload!.VenueEventId);
Assert.Equal((Guid)publishedEvent.ManifestId, payload.ManifestId);
```

## Risks and Assumptions

- Only domain events with an explicit mapping produce an outbox row;
  unmapped events are ignored. This is intentional — not every domain event
  crosses module boundaries.
- Serialization uses `System.Text.Json` with default options. If record
  constructors include non-primitive types, a custom converter may be needed
  later; for now all payload fields are `Guid` and `DateTimeOffset`.
- No migration is required. The `events.outbox_messages` table was created
  in a prior milestone.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
