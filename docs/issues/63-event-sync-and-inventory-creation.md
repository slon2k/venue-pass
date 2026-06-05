# Capability B: Event Synchronization and Inventory Creation

## Summary

Enable the Ticketing module to react to published events by subscribing to `EventPublishedIntegrationEvent`, fetching the frozen manifest via the Events module contract, and creating a local inventory with all seats and general admission pools. This is the first real cross-module integration in the codebase and validates the notify-and-fetch pattern end-to-end.

## Scope

- In scope:
  - `PublishedEventReference` entity and persistence configuration
  - `Inventory` aggregate root with `InventorySeat` and `GeneralAdmissionPool` child entities
  - `EventPublishedHandler` implementing `IIntegrationEventHandler<EventPublishedIntegrationEvent>`
  - Idempotency check (duplicate messages are no-ops)
  - EF Core migration adding Ticketing domain tables
  - Handler registration in Ticketing `ModuleConfiguration`
  - Outbox dispatcher enhancement: `GetService` → `GetServices` for multi-handler support
  - Unit tests for `Inventory.CreateFromManifest` factory
  - Unit tests for handler idempotency logic
  - Verification that cross-module DI resolution works (Events DbContext available within Ticketing handler scope)
- Out of scope:
  - Offer and pricing domain (Capability C)
  - Any Ticketing endpoints (Capabilities C/D)
  - Seat reservation or availability transitions beyond initial `Available` state
  - Ticketing publishing its own integration events

## Dependencies

- Capability A complete (Ticketing module scaffolded, `IEventsModuleContract` implemented)

## Acceptance Criteria

- [ ] `PublishedEventReference` entity exists with `EventId`, `ManifestId`, and `SyncedOnUtc`
- [ ] `Inventory` aggregate root exists with collections of `InventorySeat` and `GeneralAdmissionPool`
- [ ] `Inventory.CreateFromManifest` factory creates one seat per manifest seat and one pool per manifest GA area
- [ ] All inventory seats start with `Available` status
- [ ] All GA pools start with `AvailableCount == TotalCapacity`
- [ ] `InventorySeat` carries denormalized metadata: `SectionName`, `RowLabel`, `SeatLabel`, `SourceSeatId`
- [ ] `GeneralAdmissionPool` carries: `Name`, `TotalCapacity`, `AvailableCount`, `SourceAreaId`
- [ ] `EventPublishedHandler` creates `PublishedEventReference` + `Inventory` in a single transaction
- [ ] Handler is idempotent: if `PublishedEventReference` already exists for `EventId`, handler returns without side effects
- [ ] Handler throws if manifest fetch returns null (triggers outbox retry)
- [ ] Outbox dispatcher resolves and invokes all registered handlers for a given event type (multi-handler support)
- [ ] EF Core migration creates `published_event_references`, `inventories`, `inventory_seats`, and `general_admission_pools` tables in `ticketing` schema
- [ ] Cross-module resolution verified: `EventsModuleContract` (which uses `EventsDbContext`) resolves correctly within Ticketing handler's DI scope
- [ ] `dotnet build` and `dotnet test` pass

## Design Notes

### PublishedEventReference

```csharp
namespace VenuePass.Modules.Ticketing.Domain.PublishedEvents;

public sealed class PublishedEventReference
{
    public PublishedEventReferenceId Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid ManifestId { get; private set; }
    public DateTimeOffset SyncedOnUtc { get; private set; }

    private PublishedEventReference() { }

    public static PublishedEventReference Create(
        Guid eventId, Guid manifestId, DateTimeOffset syncedOn)
    {
        return new PublishedEventReference
        {
            Id = PublishedEventReferenceId.Create(),
            EventId = eventId,
            ManifestId = manifestId,
            SyncedOnUtc = syncedOn
        };
    }
}
```

### Inventory Aggregate

```csharp
namespace VenuePass.Modules.Ticketing.Domain.Inventories;

public sealed class Inventory : AggregateRoot<InventoryId>
{
    private readonly List<InventorySeat> _seats = [];
    private readonly List<GeneralAdmissionPool> _pools = [];

    public PublishedEventReferenceId EventReferenceId { get; private set; }
    public IReadOnlyList<InventorySeat> Seats => _seats;
    public IReadOnlyList<GeneralAdmissionPool> Pools => _pools;

    private Inventory() { }

    public static Inventory CreateFromManifest(
        PublishedEventReferenceId eventReferenceId,
        ManifestExportDto manifest)
    {
        var inventory = new Inventory
        {
            Id = InventoryId.Create(),
            EventReferenceId = eventReferenceId
        };

        foreach (var section in manifest.Sections)
            foreach (var row in section.Rows)
                foreach (var seat in row.Seats)
                    inventory._seats.Add(InventorySeat.Create(
                        inventory.Id, seat.SeatId,
                        section.Name, row.Label, seat.Label));

        foreach (var area in manifest.GeneralAdmissionAreas)
            inventory._pools.Add(GeneralAdmissionPool.Create(
                inventory.Id, area.AreaId,
                area.Name, area.Capacity));

        return inventory;
    }
}
```

### InventorySeat

```csharp
public sealed class InventorySeat
{
    public Guid Id { get; private set; }
    public InventoryId InventoryId { get; private set; }
    public Guid SourceSeatId { get; private set; }
    public string SectionName { get; private set; }
    public string RowLabel { get; private set; }
    public string SeatLabel { get; private set; }
    public SeatAvailability Availability { get; private set; }  // Available, Reserved, Sold

    public static InventorySeat Create(
        InventoryId inventoryId, Guid sourceSeatId,
        string sectionName, string rowLabel, string seatLabel)
    {
        return new InventorySeat
        {
            Id = Guid.CreateVersion7(),
            InventoryId = inventoryId,
            SourceSeatId = sourceSeatId,
            SectionName = sectionName,
            RowLabel = rowLabel,
            SeatLabel = seatLabel,
            Availability = SeatAvailability.Available
        };
    }
}

public enum SeatAvailability { Available, Reserved, Sold }
```

### GeneralAdmissionPool

```csharp
public sealed class GeneralAdmissionPool
{
    public Guid Id { get; private set; }
    public InventoryId InventoryId { get; private set; }
    public Guid SourceAreaId { get; private set; }
    public string Name { get; private set; }
    public int TotalCapacity { get; private set; }
    public int AvailableCount { get; private set; }

    public static GeneralAdmissionPool Create(
        InventoryId inventoryId, Guid sourceAreaId,
        string name, int capacity)
    {
        return new GeneralAdmissionPool
        {
            Id = Guid.CreateVersion7(),
            InventoryId = inventoryId,
            SourceAreaId = sourceAreaId,
            Name = name,
            TotalCapacity = capacity,
            AvailableCount = capacity
        };
    }
}
```

### EventPublishedHandler

```csharp
namespace VenuePass.Modules.Ticketing.IntegrationEventHandlers;

internal sealed class EventPublishedHandler(
    TicketingDbContext db,
    IEventsModuleContract eventsContract,
    TimeProvider timeProvider) : IIntegrationEventHandler<EventPublishedIntegrationEvent>
{
    public async Task Handle(
        EventPublishedIntegrationEvent integrationEvent,
        CancellationToken ct)
    {
        // Idempotency
        if (await db.PublishedEventReferences.AnyAsync(
            r => r.EventId == integrationEvent.EventId, ct))
            return;

        // Fetch manifest
        var manifest = await eventsContract.GetManifestForTicketingAsync(
            integrationEvent.ManifestId, ct);

        if (manifest is null)
            throw new InvalidOperationException(
                $"Manifest {integrationEvent.ManifestId} not available for ticketing sync.");

        // Create local reference
        var reference = PublishedEventReference.Create(
            integrationEvent.EventId,
            integrationEvent.ManifestId,
            timeProvider.GetUtcNow());

        // Create inventory
        var inventory = Inventory.CreateFromManifest(reference.Id, manifest);

        db.PublishedEventReferences.Add(reference);
        db.Inventories.Add(inventory);
        await db.SaveChangesAsync(ct);
    }
}
```

### Dispatcher Multi-Handler Enhancement

Change in `EventsOutboxDispatcher.DispatchMessageAsync`:

```csharp
// Before (single handler)
var handler = scope.ServiceProvider.GetService(handlerType);

// After (all registered handlers)
var enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerType);
var handlers = (IEnumerable<object>)scope.ServiceProvider.GetRequiredService(enumerableType);

foreach (var handler in handlers)
{
    var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.Handle))!;
    var task = (Task)method.Invoke(handler, [payload, ct])!;
    await task;
}
```

Semantics: all-or-nothing. If any handler throws, the message is not marked processed and retries. This is acceptable for M03 where there's a single handler per event type.

### DI Scope Consideration

The outbox dispatcher creates a DI scope per message. Within that scope:

- `TicketingDbContext` resolves (registered by Ticketing module)
- `IEventsModuleContract` resolves → `EventsModuleContract` resolves → `EventsDbContext` resolves (registered by Events module)

Both modules register into the same host `IServiceCollection`, so cross-module resolution works naturally. No special wiring needed.

## Vertical Slices

- [ ] B1: Implement PublishedEventReference entity, persistence configuration, and migration
- [ ] B2: Implement Inventory aggregate (InventorySeat, GeneralAdmissionPool, CreateFromManifest factory) with persistence configuration and migration
- [ ] B3: Implement EventPublishedHandler with idempotency check, manifest fetch, and inventory creation; add unit tests for handler logic
- [ ] B4: Enhance outbox dispatcher for multi-handler support; register handler in Ticketing module; verify end-to-end cross-module dispatch

## Risks and Assumptions

- Cross-module DI resolution assumes both modules are registered in the same host container (true for modular monolith, would break on extraction to separate services)
- Large manifests (thousands of seats) may result in slow bulk inserts; EF Core `AddRange` should handle reasonable sizes for M03, but production would want `BulkInsert`
- Handler throws on null manifest, relying on outbox retry. If Events module has a bug and manifest is never frozen, the message will exhaust retries and be abandoned — acceptable for M03, but monitoring should surface abandoned messages
- All-or-nothing multi-handler dispatch means a failing handler in one module blocks delivery to others. Acceptable when there's one handler per event type; needs revisiting if multiple modules subscribe to the same event
- `PublishedEventReference.EventId` should have a unique index to enforce idempotency at the database level (not just application-level `AnyAsync` check)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
