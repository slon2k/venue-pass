# Milestone 03 — Ticketing: Event Sync, Inventory & Offers

## Milestone Outcome

Ticketing module is operational: published events synchronize inventory automatically via integration events, and event managers can create and configure sellable offers with pricing.

## Delivery Model

Milestone 03 is delivered through parent feature (capability) issues and vertical-slice sub-issues. Each slice includes domain behavior, persistence impact, endpoint behavior, and tests in the same PR.

## In Scope

- [x] Capability A: Module scaffolding and Events module contract
- [x] Capability B: Event synchronization and inventory creation
- [x] Capability C: Offers and pricing
- [ ] Capability D: Inventory status query
- [ ] Capability E: Integration tests

## Capability Breakdown

### Capability A: Module scaffolding and Events module contract

- [x] A1: Scaffold Ticketing module (project, DbContext, schema, registration, initial migration)
- [x] A2: Create Events.Contracts companion project additions (IEventsModuleContract, ManifestExport DTOs)
- [x] A3: Implement IEventsModuleContract in Events module (manifest export for frozen manifests)

### Capability B: Event synchronization and inventory creation

- [x] B1: Implement PublishedEventReference domain entity and persistence
- [x] B2: Implement Inventory aggregate with InventorySeat and GeneralAdmissionPool
- [x] B3: Implement EventPublishedHandler (subscriber: idempotency check, fetch manifest, create inventory)
- [x] B4: Register handler in Ticketing module configuration and verify end-to-end dispatch, verify that cross-module handler resolution works. Add multiple handlers support.

### Capability C: Offers and pricing

- [x] C1: Implement Offer aggregate with PriceZone and status lifecycle (Draft, Active, Closed)
- [x] C2: Deliver CreateOffer endpoint and handler
- [x] C3: Deliver ConfigurePricing endpoint and handler (replace-all price zones via SetPriceZones)
- [x] C4: Deliver ActivateOffer endpoint and handler (status transition with preconditions)
- [x] C5: Deliver GetOffer and GetOffers query endpoints

### Capability D: Inventory status query

- [ ] D1: Deliver GetInventoryStatus endpoint (seat/pool availability summary)

### Capability E: Integration tests

- [ ] E1: Integration tests for event sync path (publish → inventory created, idempotency)
- [ ] E2: Integration tests for offer CRUD and pricing configuration
- [ ] E3: Integration tests for inventory status accuracy after sync

## Functional Requirements Baseline (M03)

These requirements define minimum business behavior for M03 and should be treated as implementation gates.

### Event Synchronization Requirements

- [ ] When `EventPublishedIntegrationEvent` is received, Ticketing creates a local `PublishedEventReference`.
- [ ] Ticketing fetches the frozen manifest from Events via `IEventsModuleContract`.
- [ ] Ticketing creates an `Inventory` with one `InventorySeat` per manifest seat and one `GeneralAdmissionPool` per manifest GA area.
- [ ] All seats start with `Available` status; all pools start with full available capacity.
- [ ] Handler is idempotent: duplicate `EventPublished` messages do not create duplicate inventory.
- [ ] If manifest is not available (not frozen or not found), handler throws and message retries via outbox.

### Offer and Pricing Requirements

- [ ] An Offer belongs to exactly one Inventory (one event).
- [ ] An Offer starts in `Draft` state.
- [ ] An Offer in `Draft` can have price zones replaced via the replace-all `SetPriceZones` operation.
- [ ] An Offer can be activated only if it has at least one price zone (with at least one item) configured.
- [ ] An active Offer cannot have its price zones modified.
- [ ] A PriceZone specifies a name and price (positive decimal); currency is set at the Offer level.
- [ ] A PriceZone targets either specific inventory seats (by ID list) or a GeneralAdmissionPool (by ID list).
- [ ] Multiple Offers may exist for the same event (e.g., "Early Bird", "VIP", "Standard").

### Inventory Status Requirements

- [ ] Inventory status reports total seats, available seats, and per-pool capacity/availability.
- [ ] Status reflects the current snapshot (no reservations or sales exist yet in M03).

### API Contract Requirements

- [ ] CreateOffer request includes: inventory ID, name, optional sale window.
- [ ] ConfigurePricing request includes: offer ID, price zone list (name, price, seat IDs, pool IDs).
- [ ] GetOffer response includes: offer status, name, currency, sale window, price zones with targets.
- [ ] GetInventoryStatus response includes: per-section seat counts, per-pool capacity/available.

## Accepted Decisions (Locked For M03)

1. Cross-module synchronization follows **notify-and-fetch**: thin event with IDs, consumer fetches full data via contract.
2. `IEventsModuleContract` lives in `Events.Contracts` and is implemented internally by the Events module.
3. Manifest export returns data only for frozen manifests; unfrozen returns null.
4. Inventory structure is **flat seats + separate pools**: `InventorySeat` carries section/row/seat metadata as denormalized values (no nested InventorySection/InventoryRow aggregates).
5. Offer is a separate aggregate from Inventory — independent lifecycle and commercial concerns.
6. PriceZone targeting is explicit (seat ID list or pool ID list) for M03; section-based grouping is deferred.
7. Offer status lifecycle: `Draft → Active → Closed`. No reactivation in M03.
8. Ticketing module owns its own outbox table for future integration events (e.g., `OfferActivated`), but no outbox writes are required in M03.
9. `PublishedEventReference` stores IDs only + sync timestamp. Metadata belongs to Events module
10. Use event-centric URLs for API consumers, map internally: `GET /events/{eventId}/inventory → GetInventoryStatus`
11. Throw for retry when manifest fetch fails in subscriber.
12. Offer fields = InventoryId, Name, Currency, SalesRange (DateTimeOffset? Start, DateTimeOffset? End)

## Slice Start Gate

- [ ] Functional requirements above reviewed and accepted.
- [ ] Events module contract interface design approved.
- [ ] Ticketing module project created and compiling.

## Out of Scope

- Reservation and purchase flows (Milestone 04)
- Seat locking and concurrency for reservations
- Offer sale window enforcement (active checking is structural only, not time-gated serving)
- Ticketing publishing its own integration events (deferred until needed)
- Attendance module changes
- Payment or order processing
- Dynamic pricing or surge pricing logic

## Definition of Done

- [ ] All in-scope capability issues are implemented and merged
- [ ] Integration tests validate event sync path end-to-end (publish → inventory exists)
- [ ] Unit tests validate offer/pricing domain invariants
- [ ] Handler idempotency is tested (duplicate message produces no duplicate state)
- [ ] Architecture tests pass without new module-boundary violations
- [ ] Baseline CI remains green
- [ ] Milestone and issue docs are updated to reflect completion state

## Validation Checklist

- [ ] `dotnet build` passes at solution level
- [ ] `dotnet test` passes at solution level
- [ ] Event publication in Events module triggers inventory creation in Ticketing module
- [ ] Duplicate event publication does not create duplicate inventory
- [ ] CreateOffer and GetOffer flow verified end-to-end
- [ ] ConfigurePricing attaches price levels to offer
- [ ] ActivateOffer rejects offer without price levels
- [ ] GetInventoryStatus returns correct counts after sync
- [ ] Authorization enforcement verified on Ticketing endpoints

## Risks and Dependencies

- Outbox dispatcher must correctly resolve and invoke Ticketing's `EventPublishedHandler` across module boundaries (first real cross-module dispatch)
- `IEventsModuleContract` implementation must handle the case where Events DbContext is in a different DI scope than Ticketing's handler scope
- Inventory seat count scales with manifest size; large manifests may require batch insert optimization
- Offer/PriceZone targeting model may need revision when reservations arrive in M04 — design for extensibility without over-engineering
- Integration test infrastructure from M02 must support multi-module scenarios (Ticketing + Events in same test host)
