# #41 — Ensure outbox dispatcher processes EventPublished reliably

## Summary

Implement the outbox dispatcher background service for the Events module so that
`OutboxMessage` rows written on publication are picked up, despatched to
registered `IIntegrationEventHandler<T>` implementations, and marked processed.
This is the first real execution of the outbox pattern in the codebase.

## Scope

- In scope:
  - `IIntegrationEventHandler<T>` interface in `VenuePass.BuildingBlocks.Messaging`
  - `EventsOutboxDispatcher` — a `BackgroundService` in
    `VenuePass.Modules.Events.Infrastructure.Outbox` that polls for unprocessed
    messages and dispatches them
  - Per-message error handling: `OutboxMessage.RecordFailure(...)` on exception,
    with a fixed retry delay for `NextAttemptOn`
  - DI registration of `EventsOutboxDispatcher` as a hosted service in
    `ModuleConfiguration`
  - A unit test that seeds an outbox row, executes one dispatcher tick, and
    asserts `ProcessedOn` is set
- Out of scope:
  - Real subscriber handler in the Ticketing module (Milestone 03)
  - Dead-letter queues and exponential back-off
  - Integration tests that go through the full HTTP publish path (slice C5)
  - Any new migration

## Acceptance Criteria

- [x] `IIntegrationEventHandler<T>` is declared in `VenuePass.BuildingBlocks.Messaging`
- [x] `EventsOutboxDispatcher` is a `BackgroundService` that:
  - Queries a bounded batch of `OutboxMessages` where `ProcessedOn IS NULL`
    and `NextAttemptOn <= utcNow`, ordered by `OccurredOn ASC`
  - Resolves the CLR type from `OutboxMessage.Type` and deserializes `Payload`
  - Resolves `IIntegrationEventHandler<T>` from a DI scope and invokes `Handle`
  - Opens a new DI scope **per message** so each message gets an isolated
    `DbContext` and handler dependencies
  - On success: calls `OutboxMessage.MarkProcessed(utcNow)` and saves within
    that message's scope — commit is durable before the next message begins
  - On failure: calls `OutboxMessage.RecordFailure(attemptedOn, nextAttemptOn, error)`
    and saves within that message's scope — a failure on message N does not
    roll back the committed state of messages 1 through N-1
  - Logs at minimum one line per dispatched message (type + id)
- [x] If no handler is registered for a given message type the message is still
      marked processed (skip without error)
- [x] `EventsOutboxDispatcher` is registered via `services.AddHostedService<EventsOutboxDispatcher>()`
      in `ModuleConfiguration`
- [x] A unit test (in `VenuePass.Modules.Events.Tests`) seeds one outbox row,
      calls a single dispatcher execution, and asserts `ProcessedOn != null`
- [x] `dotnet build` and `dotnet test` pass

## Design Notes

### IIntegrationEventHandler\<T\>

Add to `VenuePass.BuildingBlocks.Messaging` alongside the existing
`IIntegrationEvent`:

```csharp
public interface IIntegrationEventHandler<in T> where T : IIntegrationEvent
{
    Task Handle(T integrationEvent, CancellationToken cancellationToken);
}
```

### EventsOutboxDispatcher

Extend `BackgroundService`; inject `IServiceProvider`, `TimeProvider`, and
`ILogger<EventsOutboxDispatcher>`:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
    while (await timer.WaitForNextTickAsync(stoppingToken))
        await DispatchBatchAsync(stoppingToken);
}
```

`DispatchBatchAsync` queries the batch IDs using a short-lived scope, then
processes each message in its own dedicated DI scope. Per-message scoping
guarantees:

- Each message gets a fresh `DbContext` — no cross-message change-tracker
  state leakage.
- A commit for message N is durable before message N+1 starts — a mid-batch
  crash only risks re-delivery of the in-flight message, which is expected
  under at-least-once semantics.
- Scoped handler dependencies (repositories, DbContexts) are fully isolated
  per invocation.

### Runtime type dispatch

`OutboxMessage.Type` stores the full CLR type name
(`typeof(EventPublishedIntegrationEvent).FullName`). Resolve at runtime with:

```csharp
var type = Type.GetType(message.Type)
    ?? AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(message.Type))
        .FirstOrDefault(t => t is not null);
```

Dispatch to the handler via reflection:

```csharp
var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
var handler = scope.ServiceProvider.GetService(handlerType);
if (handler is not null)
{
    var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.Handle))!;
    await (Task)method.Invoke(handler, [payload, ct])!;
}
```

Reflection is intentional here — the dispatcher must remain open to any
registered handler without compile-time coupling. This is acceptable per TD-02.

### Unit test approach

The test does not start a real `BackgroundService` loop. Instead it calls the
extracted `DispatchBatchAsync` method directly (or invokes `ExecuteAsync` with a
short-lived cancellation token). An in-memory `EventsDbContext` is seeded with
one `OutboxMessage`; a test-double
`IIntegrationEventHandler<EventPublishedIntegrationEvent>` is registered in a
`ServiceCollection` and confirmed invoked. After dispatch, the row has
`ProcessedOn != null`.

### Retry delay

For M02, use a fixed 30-second `NextAttemptOn` offset on failure. No cap or
exponential back-off needed yet.

## Vertical Slices

- [x] Add `IIntegrationEventHandler<T>` to `BuildingBlocks.Messaging`
- [x] Implement `EventsOutboxDispatcher` (`BackgroundService` + batch query + type
      resolution + handler invoke + processed/failure marking)
- [x] Register dispatcher in `ModuleConfiguration`
- [x] Add unit test

## Risks and Assumptions

- `Type.GetType` requires the assembly to be loaded. The Events module assembly
  is always loaded in-process so this is safe for M02. Assembly-qualified names
  would be more robust but add noise to the stored type string.
- Reflection-based dispatch is acceptable for this milestone. If the number of
  integration event types grows significantly, a registry or source-generated
  approach can replace it later.
- A DI scope is opened per message. At a 5-second polling interval with a
  batch cap of ~20 messages the allocation cost (scope construction +
  `DbContext` instantiation) is negligible compared to the DB round-trip.
  Per-batch scoping would share a `DbContext` across messages, risking
  dirty change-tracker state and cross-message save bleed.

## Definition of Done

- [x] Acceptance criteria met
- [x] Tests passing
- [x] Docs updated if behavior changed
