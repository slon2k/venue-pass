# Capability D: Inventory Status Query

## Summary

Provide a read endpoint that surfaces the current inventory availability for a published event, giving event managers visibility into seat and pool counts. This is a query-only capability with no domain mutations.

## Scope

- In scope:
  - `GetInventoryStatus` endpoint returning seat and pool availability summary
  - Response grouped by section (seat counts) and individual pools (capacity/available)
  - Authorization (authenticated users)
  - Query handler projecting directly from `TicketingDbContext`
- Out of scope:
  - Individual seat-level listing (full seat map)
  - Real-time availability (no reservations or sales exist in M03 — all seats are Available)
  - Caching or read model projections
  - Filtering or pagination

## Dependencies

- Capability B complete (Inventory with seats and pools exists after event sync)

## Acceptance Criteria

- [ ] `GET /events/{eventId}/inventory` returns inventory status for a published event
- [ ] Response includes total seat count and available seat count
- [ ] Response includes per-section summary: section name, total seats, available seats
- [ ] Response includes per-pool summary: pool name, total capacity, available count
- [ ] Returns `404` if no inventory exists for the given event
- [ ] Requires authentication
- [ ] `dotnet build` and `dotnet test` pass

## Design Notes

### Endpoint

| Method | Route | Auth | Response |
| -------- | ------- | ------ | ---------- |
| GET | `/events/{eventId}/inventory` | Authenticated | 200 with status / 404 |

### Response Shape

```csharp
public sealed record InventoryStatusResponse(
    Guid EventId,
    Guid InventoryId,
    int TotalSeats,
    int AvailableSeats,
    IReadOnlyList<SectionStatusDto> Sections,
    IReadOnlyList<PoolStatusDto> Pools);

public sealed record SectionStatusDto(
    string Name,
    int TotalSeats,
    int AvailableSeats);

public sealed record PoolStatusDto(
    string Name,
    int TotalCapacity,
    int AvailableCount);
```

### Handler — Direct Projection

No aggregate loading needed. Project from DbContext:

```csharp
internal sealed class GetInventoryStatusHandler(TicketingDbContext db)
{
    public async Task<Result<InventoryStatusResponse>> Handle(
        Guid eventId, CancellationToken ct)
    {
        var reference = await db.PublishedEventReferences
            .FirstOrDefaultAsync(r => r.EventId == eventId, ct);

        if (reference is null)
            return GetInventoryStatusErrors.EventNotFound(eventId);

        var inventory = await db.Inventories
            .FirstOrDefaultAsync(i => i.EventReferenceId == reference.Id, ct);

        if (inventory is null)
            return GetInventoryStatusErrors.InventoryNotFound(eventId);

        var sections = await db.InventorySeats
            .Where(s => s.InventoryId == inventory.Id)
            .GroupBy(s => s.SectionName)
            .Select(g => new SectionStatusDto(
                g.Key,
                g.Count(),
                g.Count(s => s.Availability == SeatAvailability.Available)))
            .ToListAsync(ct);

        var pools = await db.GeneralAdmissionPools
            .Where(p => p.InventoryId == inventory.Id)
            .Select(p => new PoolStatusDto(p.Name, p.TotalCapacity, p.AvailableCount))
            .ToListAsync(ct);

        var totalSeats = sections.Sum(s => s.TotalSeats);
        var availableSeats = sections.Sum(s => s.AvailableSeats);

        return new InventoryStatusResponse(
            eventId, inventory.Id.Value,
            totalSeats, availableSeats,
            sections, pools);
    }
}
```

### Why No Aggregate Load

This is a pure read query. Loading the full `Inventory` aggregate with all seats into memory would be wasteful. Direct projection to DTOs via EF Core `Select` keeps memory and query cost proportional to the section/pool count, not the seat count.

## Vertical Slices

- [x] D1: Deliver GetInventoryStatus endpoint, handler, response DTOs, and integration test

## Risks and Assumptions

- In M03 all seats are `Available` (no reservations), so the query results are trivially correct. The real value of this endpoint emerges in M04 when availability changes
- GroupBy query relies on EF Core translating to SQL `GROUP BY` — works with SQL Server; verify in integration test
- No pagination needed: section count per venue is bounded (typically < 20 sections); pool count is bounded similarly
- Response shape is designed to remain stable when reservations are introduced — `AvailableSeats` will naturally reflect real availability without contract changes

## Definition of Done

- [x] Acceptance criteria met
- [x] Tests passing
- [x] Docs updated if behavior changed

---

## PR Description

### feat: complete Capability C (Offers & Pricing) and D (Inventory Status)

### Summary

- Implements the full offer lifecycle: create, configure pricing, activate, and query offers for a published event's inventory
- Adds `SetPriceZones` replace-all domain method with cross-zone conflict detection and inventory validation
- Delivers the inventory status query endpoint, returning seat counts grouped by section and pool availability
- Updates Capability C issue and M03 milestone docs to reflect scope adjustments made during implementation (PriceLevel → PriceZone rename, Currency lifted to Offer level, 204 response codes on mutations)

### Changes

**Domain**
- `Offer.SetPriceZones(Inventory, IReadOnlyList<PriceZoneInput>)` — atomically replaces all price zones; validates Draft status, duplicate names, empty targets, inventory membership, and cross-zone conflicts
- `PriceZoneInput` record added alongside existing input types in `PriceZone.cs`
- `OfferErrors.DuplicatePriceZoneNames()` added

**Endpoints — Capability C**

| Method | Route | Auth | Response |
|--------|-------|------|----------|
| POST | `/events/{eventId}/offers` | EventManager | 201 |
| PUT | `/offers/{offerId}/price-zones` | EventManager | 204 |
| POST | `/offers/{offerId}/activate` | EventManager | 204 |
| GET | `/offers/{offerId}` | Authenticated | 200 |
| GET | `/events/{eventId}/offers` | Authenticated | 200 |

**Endpoints — Capability D**

| Method | Route | Auth | Response |
|--------|-------|------|----------|
| GET | `/events/{eventId}/inventory` | Authenticated | 200 |

**Tests**
- 6 `SetPriceZones` domain unit tests (replace, non-draft rejection, clear, duplicate names, cross-zone conflict, unknown seat)
- 11 handler unit tests for `GetOffer` and `GetOffers` (SQLite in-memory — required because EF Core InMemory does not support `ComplexProperty` used by `Offer.SalesRange`)
- 5 handler unit tests for `GetInventoryStatus` (section grouping, pool mapping, zero counts, total sum)

**Infrastructure**
- FluentValidation packages added to Ticketing module
- SQLite added to Ticketing unit test project (for handler tests)
- All handlers and endpoints registered in `ModuleConfiguration` and `ModuleEndpointMappings`

**Docs**
- `docs/issues/68-offers-and-pricing.md` — updated to PriceZone terminology, corrected response codes (204), all acceptance criteria and slices marked complete
- `docs/milestones/milestone-03.md` — Capabilities A, B, C marked complete; C1/C3 descriptions updated; accepted decisions #6, #7, #12 corrected

### Test plan

- [ ] `dotnet build` passes at solution root
- [ ] `dotnet test` — 210 tests pass (164 non-Docker); Events integration tests require Docker and are excluded from local runs
- [ ] `POST /events/{eventId}/offers` with valid body returns 201 with offer ID
- [ ] `PUT /offers/{offerId}/price-zones` with price zone list returns 204; second call with different list replaces zones
- [ ] `PUT /offers/{offerId}/price-zones` on an Active offer returns 400
- [ ] `POST /offers/{offerId}/activate` with no price zones returns 400; with zones returns 204
- [ ] `GET /offers/{offerId}` returns offer with price zones and seat/pool IDs
- [ ] `GET /events/{eventId}/offers` returns all offers for the event
- [ ] `GET /events/{eventId}/inventory` returns section counts and pool availability
- [ ] All mutation endpoints return 401/403 without valid EventManager token
