# Capability C: Offers and Pricing

## Summary

Enable event managers to create sellable offers for a published event's inventory and configure pricing. Offers represent commercial products (e.g., "Early Bird", "VIP", "Standard") with price zones that target specific seats or general admission pools. This capability delivers the full CRUD lifecycle for offers including status transitions from Draft through Active.

## Scope

- In scope:
  - `Offer` aggregate root with `PriceZone` child entities
  - Offer status lifecycle: `Draft → Active → Closed`
  - `CreateOffer` endpoint and handler
  - `ConfigurePricing` endpoint and handler (replace-all price zones on offer)
  - `ActivateOffer` endpoint and handler (status transition with preconditions)
  - `GetOffer` and `GetOffers` query endpoints
  - EF Core persistence configuration and migration
  - Validation (FluentValidation for request input)
  - Authorization (EventManager role required for mutations)
  - Unit tests for offer domain invariants
- Out of scope:
  - Offer sale window time-gated enforcement (structure captured, not enforced at query time)
  - Closing an offer (deferred — no `CloseOffer` endpoint in M03)
  - Seat-level conflict detection (same seat in multiple offers — allowed in M03, revisited in M04)
  - Reservation or purchase flows
  - Ticketing outbox writes (no integration events published)

## Dependencies

- Capability B complete (Inventory exists with seats and pools for a published event)

## Acceptance Criteria

- [x] `Offer` aggregate exists with `InventoryId`, `Name`, `Currency`, `Status`, optional `SalesRange` (start/end), and collection of `PriceZone`
- [x] Offer starts in `Draft` status on creation
- [x] `PriceZone` has `Name`, `Price` (positive decimal), and targeting (seat items or pool items)
- [x] Price zones can be added to a Draft offer
- [x] Price zones can be replaced on a Draft offer (replace-all via `SetPriceZones`)
- [x] Price zones cannot be modified on an Active offer (domain rule)
- [x] Offer can be activated only if at least one price zone (with at least one item) exists
- [x] Activating an offer transitions status from `Draft` to `Active`
- [x] Activating a non-Draft offer is rejected
- [x] `CreateOffer` endpoint: `POST /events/{eventId}/offers` → 201 with offer ID
- [x] `ConfigurePricing` endpoint: `PUT /offers/{offerId}/price-zones` → 204
- [x] `ActivateOffer` endpoint: `POST /offers/{offerId}/activate` → 204
- [x] `GetOffer` endpoint: `GET /offers/{offerId}` → 200 with offer details including price zones
- [x] `GetOffers` endpoint: `GET /events/{eventId}/offers` → 200 with list of offers
- [x] All mutation endpoints require `EventManager` role
- [x] Query endpoints require authentication
- [x] Validation rejects: missing name, non-positive price, empty currency, empty seat target list
- [x] `dotnet build` and `dotnet test` pass

## Design Notes

### Offer Aggregate

```csharp
namespace VenuePass.Modules.Ticketing.Domain.Offers;

public sealed class Offer : AggregateRoot<OfferId>
{
    private readonly List<PriceZone> _priceZones = [];

    public InventoryId InventoryId { get; private set; }
    public OfferName Name { get; private set; }
    public Currency Currency { get; private set; }
    public OfferStatus Status { get; private set; }
    public DateTimeRange SalesRange { get; private set; }
    public IReadOnlyList<PriceZone> PriceZones => _priceZones.AsReadOnly();

    public static Offer Create(
        InventoryId inventoryId,
        OfferName name,
        DateTimeRange salesRange,
        Currency currency) { ... }

    // Upsert a single price zone by name (case-insensitive); validates inventory
    // membership and cross-zone target conflicts against existing zones.
    public void ConfigurePriceZone(
        Inventory inventory,
        PriceZoneName name,
        Amount price,
        IEnumerable<PriceZoneInventorySeatItemInput> seatItems,
        IEnumerable<PriceZoneGeneralAdmissionPoolItemInput> poolItems) { ... }

    // Replace-all: atomically clears and sets the full price zone collection.
    public void SetPriceZones(
        Inventory inventory,
        IReadOnlyList<PriceZoneInput> inputs) { ... }

    public void Activate() { ... }
}

public enum OfferStatus { Draft, Active, Closed }
```

> **Scope adjustments from original design:** `PriceLevel` was renamed to `PriceZone` for clarity. `Currency` was lifted to the `Offer` level (one currency per offer, not per zone). `State`/`OfferState` became `Status`/`OfferStatus`. `SaleStart`/`SaleEnd` became `DateTimeRange SalesRange` (both optional). The original `PriceLevelTarget` value object was replaced by separate `PriceZoneInventorySeatItem` and `PriceZoneGeneralAdmissionPoolItem` entity types.

### PriceZone

```csharp
namespace VenuePass.Modules.Ticketing.Domain.Offers;

public sealed class PriceZone : Entity<PriceZoneId>
{
    public PriceZoneName Name { get; private set; }
    public Amount Price { get; private set; }
    public IReadOnlyList<PriceZoneInventorySeatItem> InventorySeatItems { get; }
    public IReadOnlyList<PriceZoneGeneralAdmissionPoolItem> GeneralAdmissionPoolItems { get; }
    public bool HasItems { get; }

    internal static PriceZone Create(
        PriceZoneName name,
        Amount price,
        IReadOnlyCollection<PriceZoneInventorySeatItemInput> seatItems,
        IReadOnlyCollection<PriceZoneGeneralAdmissionPoolItemInput> poolItems) { ... }
}
```

Key value objects and types in `PriceZone.cs`: `PriceZoneName`, `Currency`, `Amount`, `PriceZoneInventorySeatItem`, `PriceZoneGeneralAdmissionPoolItem`, `PriceZoneInventorySeatItemInput`, `PriceZoneGeneralAdmissionPoolItemInput`, `PriceZoneInput`.

### Endpoint Structure

| Feature | Method | Route | Auth | Response |
| --- | --- | --- | --- | --- |
| CreateOffer | POST | `/events/{eventId}/offers` | EventManager | 201 |
| ConfigurePricing | PUT | `/offers/{offerId}/price-zones` | EventManager | 204 |
| ActivateOffer | POST | `/offers/{offerId}/activate` | EventManager | 204 |
| GetOffer | GET | `/offers/{offerId}` | Authenticated | 200 |
| GetOffers | GET | `/events/{eventId}/offers` | Authenticated | 200 |

### CreateOffer Handler — Inventory Lookup

`CreateOffer` receives `eventId` in the route. The handler resolves the inventory:

```csharp
var reference = await db.PublishedEventReferences
    .FirstOrDefaultAsync(r => r.EventId == command.EventId, ct);

if (reference is null)
    return CreateOfferErrors.EventNotPublished(command.EventId);

var inventory = await db.Inventories
    .FirstOrDefaultAsync(i => i.EventReferenceId == reference.Id, ct);

if (inventory is null)
    return CreateOfferErrors.InventoryNotFound(command.EventId);

var offer = Offer.Create(inventory.Id, new OfferName(command.Name),
    new DateTimeRange(command.SaleStart, command.SaleEnd), new Currency(command.Currency));
```

### ConfigurePricing — Replace Strategy

`SetPriceZones` uses a replace-all strategy: the request sends the full desired set of price zones, which atomically replaces the current set. The handler:

1. Loads the offer (`PriceZones` auto-loaded via `OwnsMany`)
2. Loads the inventory (`Seats` and `Pools` auto-loaded via `OwnsMany`)
3. Calls `offer.SetPriceZones(inventory, inputs)`
4. Saves — EF handles orphan deletion via change tracking on the owned collection

The domain validates: Draft status, no duplicate zone names (case-insensitive), each zone targets at least one item, all seat/pool IDs exist in the inventory, no cross-zone target conflicts.

## Vertical Slices

- [x] C1: Implement Offer aggregate with PriceZone, PriceZoneInput, and status lifecycle; add persistence configuration and migration; add unit tests for domain invariants
- [x] C2: Deliver CreateOffer endpoint, handler, and validator
- [x] C3: Deliver ConfigurePricing endpoint, handler, and validator (replace-all strategy via SetPriceZones)
- [x] C4: Deliver ActivateOffer endpoint and handler (status transition with precondition checks)
- [x] C5: Deliver GetOffer and GetOffers query endpoints

## Risks and Assumptions

- Replace-all pricing strategy simplifies M03 but may feel heavy for UIs that add one price zone at a time; `ConfigurePriceZone` (per-zone upsert) remains available on the aggregate for incremental use in future slices
- `PriceZoneInventorySeatItem` and `PriceZoneGeneralAdmissionPoolItem` are stored in separate junction tables; EF Core `OwnsMany` handles orphan deletion automatically on replace-all
- Same seat can appear in multiple offers' price zones — no conflict detection in M03. Multiple offers targeting overlapping seats is valid (e.g., "Early Bird" and "Standard" for same section). M04 reservation logic will enforce actual availability
- `SalesRange` is stored but not enforced in queries — informational in M03. Sale window enforcement comes with the purchase flow
- EventManager role is sufficient for all mutations — no ownership check. Ownership enforcement deferred to when it matters commercially

## Definition of Done

- [x] Acceptance criteria met
- [x] Tests passing
- [x] Docs updated if behavior changed
