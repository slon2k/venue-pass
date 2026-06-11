# Milestone 03 — Ticketing: Event Sync, Inventory & Offers

## Milestone Outcome

Ticketing module is operational: published events synchronize inventory automatically via integration events, and event managers can create and configure sellable offers with price zones.

## Delivery Model

Milestone 03 is delivered through parent feature (capability) issues and vertical-slice sub-issues. Each slice includes domain behavior, persistence impact, endpoint behavior, and tests in the same PR.

## In Scope

- [x] Capability A: Module scaffolding and Events module contract
- [x] Capability B: Event synchronization and inventory creation
- [x] Capability C: Offers and pricing
- [x] Capability D: Inventory status query
- [x] Capability E: Integration tests

## Capability Breakdown

### Capability A: Module scaffolding and Events module contract

- [x] A1: Scaffold Ticketing module (project, DbContext, schema, registration, initial migration)
- [x] A2: Create Events.Contracts companion project additions (`IEventsModuleContract`, ManifestExport DTOs)
- [x] A3: Implement `IEventsModuleContract` in Events module (manifest export for frozen manifests)

### Capability B: Event synchronization and inventory creation

- [x] B1: Implement `PublishedEventReference` domain entity and persistence
- [x] B2: Implement `Inventory` aggregate with `InventorySeat` and `GeneralAdmissionPool`
- [x] B3: Implement `EventPublishedHandler` (subscriber: idempotency check, fetch manifest, create inventory)
- [x] B4: Register handler in Ticketing module configuration and verify end-to-end dispatch. Verify that cross-module handler resolution works. Add multiple handlers support.

### Capability C: Offers and pricing

- [x] C1: Implement `Offer` aggregate with `PriceZone` and status lifecycle (`Draft`, `Active`, `Closed`)
- [x] C2: Deliver `CreateOffer` endpoint and handler
- [x] C3: Deliver `ConfigurePricing` endpoint and handler for configuring offer price zones
- [x] C4: Deliver `ActivateOffer` endpoint and handler (status transition with preconditions)
- [x] C5: Deliver `GetOffer` and `GetOffers` query endpoints

### Capability D: Inventory status query

- [x] D1: Deliver `GetInventoryStatus` endpoint (seat/pool availability summary)

### Capability E: Integration tests

- [x] E1: Integration tests for event sync path (publish → inventory created, idempotency)
- [x] E2: Integration tests for offer creation, querying, pricing configuration, and activation
- [x] E3: Integration tests for inventory status accuracy after sync

## Functional Requirements Baseline (M03)

These requirements define minimum business behavior for M03 and should be treated as implementation gates.

### Event Synchronization Requirements

- [x] When `EventPublishedIntegrationEvent` is received, Ticketing creates a local `PublishedEventReference`.
- [x] Ticketing fetches the frozen manifest from Events via `IEventsModuleContract`.
- [x] Ticketing creates an `Inventory` with one `InventorySeat` per manifest seat and one `GeneralAdmissionPool` per manifest GA area.
- [x] All seats start with `Available` status; all pools start with full available capacity.
- [x] Handler is idempotent: duplicate `EventPublished` messages do not create duplicate inventory.
- [x] If manifest is not available (not frozen or not found), handler throws and message retries via outbox.

### Offer and Pricing Requirements

- [x] An Offer belongs to exactly one Inventory (one event).
- [x] An Offer starts in `Draft` state.
- [x] An Offer in `Draft` can configure price zones.
- [x] Configuring a price zone with the same name replaces the existing zone.
- [x] An Offer can be activated only if it has at least one price zone configured.
- [x] Every active Offer price zone must contain at least one target.
- [x] An active Offer cannot have its price zones modified.
- [x] A `PriceZone` specifies a name and non-negative price; currency is set at the Offer level.
- [x] Price is defined at the `PriceZone` level, not per individual seat/pool target.
- [x] A `PriceZone` targets one or more inventory seats and/or general admission pools by ID.
- [x] Within one Offer, an inventory seat or general admission pool can belong to at most one `PriceZone`.
- [x] Multiple Offers may exist for the same event/inventory (e.g., "Early Bird", "VIP", "Standard").

### Inventory Status Requirements

- [x] Inventory status reports total seats, available seats, and per-pool capacity/availability.
- [x] Status reflects the current snapshot (no reservations or sales exist yet in M03).

### API Contract Requirements

- [x] `CreateOffer` request includes: inventory ID, name, currency, optional sale window.
- [x] `ConfigurePricing` request includes: offer ID, price zone list/name, price, seat IDs, pool IDs.
- [x] `GetOffer` response includes: offer status, name, currency, sale window, price zones with targets.
- [x] `GetInventoryStatus` response includes: per-section seat counts, per-pool capacity/available.

## Accepted Decisions (Locked For M03)

1. Cross-module synchronization follows **notify-and-fetch**: thin event with IDs, consumer fetches full data via contract.
2. `IEventsModuleContract` lives in `Events.Contracts` and is implemented internally by the Events module.
3. Manifest export returns data only for frozen manifests; unfrozen returns null.
4. Inventory structure is **flat seats + separate pools**: `InventorySeat` carries section/row/seat metadata as denormalized values (no nested InventorySection/InventoryRow aggregates).
5. Offer is a separate aggregate from Inventory — independent lifecycle and commercial concerns.
6. PriceZone targeting is explicit (seat ID list and/or pool ID list) for M03; section-based grouping is deferred.
7. Offer status lifecycle: `Draft → Active → Closed`. No reactivation in M03.
8. Ticketing module owns its own outbox table for future integration events (e.g., `OfferActivated`), but no outbox writes are required in M03.
9. `PublishedEventReference` stores IDs only + sync timestamp. Metadata belongs to Events module.
10. Use event-centric URLs for API consumers, map internally: `GET /events/{eventId}/inventory → GetInventoryStatus`.
11. Throw for retry when manifest fetch fails in subscriber.
12. Offer fields = InventoryId, Name, Currency, SalesRange (`DateTimeOffset? Start`, `DateTimeOffset? End`).
13. Pricing model is single-axis for M03: `Offer + PriceZones`, with one price per zone.
14. Within one Offer, an inventory seat or general admission pool can belong to at most one `PriceZone`.
15. Price is defined at the `PriceZone` level, not per individual seat/pool target.
16. Advanced two-axis matrix pricing (`Offer + Target + PriceType`, e.g. Adult/Child/Senior) is deferred.

## Slice Start Gate

- [x] Functional requirements above reviewed and accepted.
- [x] Events module contract interface design approved.
- [x] Ticketing module project created and compiling.

## Out of Scope

- Reservation and purchase flows (Milestone 04)
- Seat locking and concurrency for reservations
- Offer sale window enforcement (active checking is structural only, not time-gated serving)
- Ticketing publishing its own integration events (deferred until needed)
- Attendance module changes
- Payment or order processing
- Dynamic pricing or surge pricing logic
- Two-axis matrix pricing (`PriceType` dimension such as Adult/Child/Senior)

## Definition of Done

- [x] All in-scope capability issues are implemented and merged
- [x] Integration tests validate event sync path end-to-end (publish → inventory exists)
- [x] Unit tests validate offer/pricing domain invariants
- [x] Handler idempotency is tested (duplicate message produces no duplicate state)
- [x] Architecture tests pass without new module-boundary violations
- [x] Baseline CI remains green
- [x] Milestone and issue docs are updated to reflect completion state

## Validation Checklist

- [x] `dotnet build` passes at solution level
- [x] `dotnet test` passes at solution level
- [x] Event publication in Events module triggers inventory creation in Ticketing module
- [x] Duplicate event publication does not create duplicate inventory
- [x] CreateOffer and GetOffer flow verified end-to-end
- [x] ConfigurePricing attaches price zones to offer
- [x] ActivateOffer rejects offer without price zones
- [x] ActivateOffer rejects offer with empty price zones
- [x] GetInventoryStatus returns correct counts after sync
- [x] Authorization enforcement verified on Ticketing endpoints

## Closure State

- Milestone status: Closed
- Closed on: 2026-06-10

## Retrospective Highlights

### What Worked Well

- Module boundaries held up under real cross-module flows (Events → Ticketing) with contracts and architecture tests enforcing separation.
- Notify-and-fetch integration pattern proved practical for this stage: thin event payloads with explicit contract fetches kept ownership clear.
- Offer and pricing functionality reached completeness for M03 scope without over-modeling.
- Integration test project consolidation improved confidence in end-to-end behavior and reduced ambiguity in CI coverage.

### Key Decisions Validated

- Keeping pricing single-axis (`Offer + PriceZones`) was the right scope tradeoff for M03.
- Renaming `PriceLevel` to `PriceZone` reduced ambiguity and clarified that pricing is based on inventory grouping.
- Event-centric API routes improved external clarity while preserving internal model mapping.
- SQL-backed integration tests in CI were essential for catching wiring and persistence regressions.

### Pain Points Observed

- Cross-module integration tests have a larger failure surface and require careful fixture/setup discipline.
- Documentation drift can occur late in milestone execution unless closure updates are done immediately after feature completion.
- CI path sensitivity (project renames/moves) can break pipelines even when implementation is correct.

### Carry Forward to M04

- Prioritize reservation correctness and concurrency boundaries before adding richer pricing features.
- Keep contracts minimal and explicit; avoid introducing shared internal abstractions across modules.
- Maintain strong integration coverage for eventual consistency paths and idempotency behaviors.
- Add a checklist item to all milestone closure flows to re-validate CI paths after project structure changes.
- Revisit pricing only if reservation/checkout requirements prove that a second pricing axis is necessary.

## Risks / Carry-Forward Notes

- Inventory seat count scales with manifest size; large manifests may require batch insert optimization.
- Offer/PriceZone targeting model may need revision when reservations arrive in M04 — design for extensibility without over-engineering.
- Reservation implementation must respect current pricing assumption: within one Offer, a target resolves to at most one PriceZone/price.
- Integration test infrastructure from M02/M03 must continue supporting multi-module scenarios.