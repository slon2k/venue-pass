# Capability C: Offers and Pricing

## Summary

Enable event managers to create sellable offers for a published event's inventory and configure pricing. Offers represent commercial products (e.g., "Early Bird", "VIP", "Standard") with price levels that target specific seats or general admission pools. This capability delivers the full CRUD lifecycle for offers including state transitions from Draft through Active.

## Scope

- In scope:
  - `Offer` aggregate root with `PriceLevel` child entities
  - Offer state lifecycle: `Draft → Active → Closed`
  - `CreateOffer` endpoint and handler
  - `ConfigurePricing` endpoint and handler (add/update/remove price levels)
  - `ActivateOffer` endpoint and handler (state transition with preconditions)
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

- [ ] `Offer` aggregate exists with `InventoryId`, `Name`, `State`, optional `SaleStart`/`SaleEnd`, and collection of `PriceLevel`
- [ ] Offer starts in `Draft` state on creation
- [ ] `PriceLevel` has `Name`, `Price` (positive decimal), `Currency`, and targeting (seat IDs or pool ID)
- [ ] Price levels can be added to a Draft offer
- [ ] Price levels can be updated or removed from a Draft offer
- [ ] Price levels cannot be modified on an Active offer (domain rule)
- [ ] Offer can be activated only if at least one price level exists
- [ ] Activating an offer transitions state from `Draft` to `Active`
- [ ] Activating a non-Draft offer is rejected
- [ ] `CreateOffer` endpoint: `POST /events/{eventId}/offers` → 201 with offer ID
- [ ] `ConfigurePricing` endpoint: `PUT /offers/{offerId}/price-levels` → 200
- [ ] `ActivateOffer` endpoint: `POST /offers/{offerId}/activate` → 200
- [ ] `GetOffer` endpoint: `GET /offers/{offerId}` → 200 with offer details including price levels
- [ ] `GetOffers` endpoint: `GET /events/{eventId}/offers` → 200 with list of offers
- [ ] All mutation endpoints require `EventManager` role
- [ ] Query endpoints require authentication
- [ ] Validation rejects: missing name, non-positive price, empty currency, empty seat target list
- [ ] `dotnet build` and `dotnet test` pass

## Design Notes

### Offer Aggregate

```csharp
namespace VenuePass.Modules.Ticketing.Domain.Offers;

public sealed class Offer : AggregateRoot<OfferId>
{
    private readonly List<PriceLevel> _priceLevels = [];

    public InventoryId InventoryId { get; private set; }
    public string Name { get; private set; }
    public OfferState State { get; private set; }
    public DateTimeOffset? SaleStart { get; private set; }
    public DateTimeOffset? SaleEnd { get; private set; }
    public IReadOnlyList<PriceLevel> PriceLevels => _priceLevels;

    private Offer() { }

    public static Offer Create(
        InventoryId inventoryId,
        string name,
        DateTimeOffset? saleStart,
        DateTimeOffset? saleEnd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (saleStart.HasValue && saleEnd.HasValue && saleEnd <= saleStart)
            throw new DomainRuleViolationException("Sale end must be after sale start.");

        return new Offer
        {
            Id = OfferId.Create(),
            InventoryId = inventoryId,
            Name = name,
            State = OfferState.Draft,
            SaleStart = saleStart,
            SaleEnd = saleEnd
        };
    }

    public void SetPriceLevels(IReadOnlyList<PriceLevel> priceLevels)
    {
        if (State != OfferState.Draft)
            throw new DomainRuleViolationException("Cannot modify pricing on a non-draft offer.");

        _priceLevels.Clear();
        _priceLevels.AddRange(priceLevels);
    }

    public void Activate()
    {
        if (State != OfferState.Draft)
            throw new DomainRuleViolationException("Only draft offers can be activated.");

        if (_priceLevels.Count == 0)
            throw new DomainRuleViolationException("Cannot activate an offer without price levels.");

        State = OfferState.Active;
    }
}

public enum OfferState { Draft, Active, Closed }
```

### PriceLevel

```csharp
namespace VenuePass.Modules.Ticketing.Domain.Offers;

public sealed class PriceLevel
{
    public Guid Id { get; private set; }
    public OfferId OfferId { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; }
    public PriceLevelTarget Target { get; private set; }

    private PriceLevel() { }

    public static PriceLevel Create(
        OfferId offerId,
        string name,
        decimal price,
        string currency,
        PriceLevelTarget target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (price <= 0)
            throw new DomainRuleViolationException("Price must be positive.");

        if (target is null || target.IsEmpty)
            throw new DomainRuleViolationException("Price level must target at least one seat or pool.");

        return new PriceLevel
        {
            Id = Guid.CreateVersion7(),
            OfferId = offerId,
            Name = name,
            Price = price,
            Currency = currency,
            Target = target
        };
    }
}
```

### PriceLevelTarget (Value Object)

```csharp
namespace VenuePass.Modules.Ticketing.Domain.Offers;

public sealed class PriceLevelTarget
{
    public IReadOnlyList<Guid> SeatIds { get; }
    public Guid? PoolId { get; }
    public bool IsEmpty => SeatIds.Count == 0 && PoolId is null;

    private PriceLevelTarget(IReadOnlyList<Guid> seatIds, Guid? poolId)
    {
        SeatIds = seatIds;
        PoolId = poolId;
    }

    public static PriceLevelTarget ForSeats(IReadOnlyList<Guid> seatIds)
    {
        if (seatIds.Count == 0)
            throw new DomainRuleViolationException("Seat target must include at least one seat.");
        return new PriceLevelTarget(seatIds, null);
    }

    public static PriceLevelTarget ForPool(Guid poolId)
    {
        return new PriceLevelTarget([], poolId);
    }
}
```

### Endpoint Structure

| Feature | Method | Route | Auth |
| --------- | -------- | ------- | ------ |
| CreateOffer | POST | `/events/{eventId}/offers` | EventManager |
| ConfigurePricing | PUT | `/offers/{offerId}/price-levels` | EventManager |
| ActivateOffer | POST | `/offers/{offerId}/activate` | EventManager |
| GetOffer | GET | `/offers/{offerId}` | Authenticated |
| GetOffers | GET | `/events/{eventId}/offers` | Authenticated |

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

var offer = Offer.Create(inventory.Id, command.Name, command.SaleStart, command.SaleEnd);
```

### ConfigurePricing — Replace Strategy

`SetPriceLevels` uses a replace-all strategy: the request sends the full list of price levels. This avoids complex add/remove/update individual operations for M03. The handler:

1. Loads the offer with existing price levels
2. Validates the new price levels (targets exist in inventory)
3. Calls `offer.SetPriceLevels(newLevels)`
4. Saves (EF handles orphan deletion)

### Target Validation

The handler (not the domain) validates that targeted seat IDs and pool IDs exist in the offer's inventory:

```csharp
var inventorySeatIds = await db.InventorySeats
    .Where(s => s.InventoryId == offer.InventoryId)
    .Select(s => s.Id)
    .ToHashSetAsync(ct);

foreach (var target in command.PriceLevels.SelectMany(pl => pl.SeatIds))
{
    if (!inventorySeatIds.Contains(target))
        return ConfigurePricingErrors.SeatNotInInventory(target);
}
```

## Vertical Slices

- [ ] C1: Implement Offer aggregate with PriceLevel, PriceLevelTarget, and state lifecycle; add persistence configuration and migration; add unit tests for domain invariants
- [ ] C2: Deliver CreateOffer endpoint, handler, and validator
- [ ] C3: Deliver ConfigurePricing endpoint, handler, and validator (replace-all strategy with target validation)
- [ ] C4: Deliver ActivateOffer endpoint and handler (state transition with precondition checks)
- [ ] C5: Deliver GetOffer and GetOffers query endpoints

## Risks and Assumptions

- Replace-all pricing strategy simplifies M03 but may feel heavy for UIs that add one price level at a time; acceptable for now, individual add/remove can be added later
- `PriceLevelTarget` stored as JSON column or junction table — EF Core owned entities with JSON serialization is simplest for M03; switch to junction table if query performance requires it
- Same seat can appear in multiple offers' price levels — no conflict detection in M03. This is intentional: multiple offers targeting overlapping seats is valid (e.g., "Early Bird" and "Standard" for same section, only one active at a time). M04 reservation logic will enforce actual availability
- `SaleStart`/`SaleEnd` are stored but not enforced in queries — they're informational in M03. Sale window enforcement (hiding non-active offers from buyers) comes with the purchase flow
- EventManager role is sufficient for all mutations — no ownership check (any EventManager can modify any offer). Ownership enforcement deferred to when it matters commercially

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
