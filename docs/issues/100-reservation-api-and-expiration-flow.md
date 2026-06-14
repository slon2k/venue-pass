# Capability B: Reservation API and Expiration Flow

## Summary

Expose the `Reservation` aggregate via HTTP and implement the expiration lifecycle. Customers can create, view, and cancel reservations. A background sweep expires overdue reservations automatically. All state-changing commands release inventory when appropriate.

## Scope

- In scope:
  - `POST /reservations` — create a reservation from an active offer
  - `GET /reservations/{id}` — retrieve a reservation with all items
  - `DELETE /reservations/{id}` — cancel a reserved reservation
  - `ExpireReservation` command/handler (no HTTP endpoint; called by the worker)
  - `ReservationExpirationWorker` background sweep
  - Inventory release on cancel and expire
  - `TicketingOptions` strongly-typed configuration class
  - Integration and concurrency tests (E1–E3, E5, E7 from milestone-04)
- Out of scope:
  - Checkout/order creation (Capability C)
  - Ticket issuance (Capability D)
  - Authorization of `ExpireReservation` as an HTTP endpoint

## Architecture Plan

Full implementation plan: [`docs/plans/milestone-04-capability-b.md`](../plans/milestone-04-capability-b.md)

## Endpoints

| Method | Path | Auth | Success |
|--------|------|------|---------|
| `POST` | `/reservations` | Any authenticated role | `201 Created` |
| `GET` | `/reservations/{reservationId:guid}` | Any authenticated role | `200 OK` |
| `DELETE` | `/reservations/{reservationId:guid}` | Any authenticated role | `204 No Content` |

## Request and Response Contracts

### `POST /reservations`

**Request body:**

```json
{
  "offerId": "guid",
  "seatIds": ["guid", "guid"],
  "gaPoolSelections": [
    { "poolId": "guid", "quantity": 3 }
  ]
}
```

**Response `201 Created`:**

```json
{
  "reservationId": "guid",
  "status": "Reserved",
  "expiresAt": "2026-06-12T10:15:00+00:00",
  "currency": "USD",
  "total": 175.00,
  "items": [
    {
      "reservationItemId": "guid",
      "type": "Seat",
      "inventorySeatId": "guid",
      "generalAdmissionPoolId": null,
      "priceZoneId": "guid",
      "quantity": 1,
      "unitPrice": 50.00,
      "total": 50.00
    },
    {
      "reservationItemId": "guid",
      "type": "GeneralAdmissionPool",
      "inventorySeatId": null,
      "generalAdmissionPoolId": "guid",
      "priceZoneId": "guid",
      "quantity": 3,
      "unitPrice": 25.00,
      "total": 75.00
    }
  ]
}
```

### `GET /reservations/{reservationId}`

**Response `200 OK`:** Same shape as the create response, plus `offerId` and `inventoryId`.

### `DELETE /reservations/{reservationId}`

**Response `204 No Content`** on success.

## Error Paths

| Scenario | HTTP Status |
|----------|-------------|
| Offer not found | 404 |
| Offer not active | 409 |
| Offer not on sale (sale window closed or not yet open) | 409 |
| Seat/pool not covered by any price zone | 400 |
| Seat not available (already reserved or sold) | 409 |
| GA pool quantity exceeds availability | 409 |
| Duplicate seat IDs in request | 400 |
| Duplicate pool selections in request | 400 |
| Concurrency conflict on inventory row | 409 |
| Reservation not found | 404 |
| Reservation in terminal state (cancel/expire on non-Reserved) | 409 |

## Expiration Background Worker

`ReservationExpirationWorker` is a `BackgroundService` that runs a periodic sweep:

1. Creates a new `IServiceScope` per iteration.
2. Queries `reservations` where `status = 'Reserved' AND expires_at < now`.
3. Calls `ExpireReservationHandler` for each candidate ID.
4. Logs and continues on per-reservation failures (concurrency conflicts, already-transitioned states).
5. Sleeps `Ticketing:ExpirationSweepInterval` (default 60 s) between runs.

The worker is **suppressed in all integration tests** via the existing `services.RemoveAll<IHostedService>()` pattern in `EventsApiFactory`. Tests that verify expiration behavior call `ExpireReservationHandler` directly through the DI container.

## Inventory Release Pattern

Used identically by both `CancelReservation` and `ExpireReservation`:

1. Collect `InventorySeatId` values from items where `Type == Seat`. If any → `inventory.ReleaseSeats(seatIds)`.
2. For each item where `Type == GeneralAdmissionPool` → `inventory.ReleaseGeneralAdmissionPool(poolId, quantity)`.
3. Commit reservation state change + inventory changes in a **single `SaveChangesAsync`**.

## Concurrency Design

All commands load `Reservation` and `Inventory` as **tracked** EF Core entities. A single `db.SaveChangesAsync()` writes all changes in one SQL transaction. SQL Server `rowversion` tokens on `reservations`, `inventory_seats`, and `inventory_pools` detect concurrent updates — the losing writer receives `DbUpdateConcurrencyException` which maps to `Error.Concurrency` → HTTP 409.

## Configuration

New strongly-typed options class `TicketingOptions`:

| Property | Type | Default | Config Key |
|----------|------|---------|-----------|
| `ReservationExpiryMinutes` | `int` | `15` | `Ticketing:ReservationExpiryMinutes` |
| `ExpirationSweepInterval` | `TimeSpan` | `00:01:00` | `Ticketing:ExpirationSweepInterval` |

`CreateReservation` uses `ReservationExpiry` (derived from `ReservationExpiryMinutes`) to compute `ExpiresAt = now + expiry`.

## Files

### New Source Files

| File | Purpose |
|------|---------|
| `TicketingOptions.cs` | Strongly-typed options bound from `"Ticketing"` config section |
| `Features/CreateReservation/CreateReservationErrors.cs` | Typed error factory |
| `Features/CreateReservation/CreateReservationValidator.cs` | FluentValidation rules |
| `Features/CreateReservation/CreateReservation.cs` | Handler, command, result records |
| `Features/CreateReservation/CreateReservationEndpoint.cs` | `POST /reservations` |
| `Features/GetReservation/GetReservationErrors.cs` | Typed error factory |
| `Features/GetReservation/GetReservation.cs` | Handler, query, result records |
| `Features/GetReservation/GetReservationEndpoint.cs` | `GET /reservations/{id}` |
| `Features/CancelReservation/CancelReservationErrors.cs` | Typed error factory |
| `Features/CancelReservation/CancelReservation.cs` | Handler and command |
| `Features/CancelReservation/CancelReservationEndpoint.cs` | `DELETE /reservations/{id}` |
| `Features/ExpireReservation/ExpireReservationErrors.cs` | Typed error factory |
| `Features/ExpireReservation/ExpireReservation.cs` | Handler and command (no endpoint) |
| `Infrastructure/ReservationExpirationWorker.cs` | Background sweep service |

### Modified Source Files

| File | Change |
|------|--------|
| `ModuleConfiguration.cs` | Register `TicketingOptions`, worker, and 4 new handlers |
| `ModuleEndpointMappings.cs` | Wire `MapCreateReservation`, `MapGetReservation`, `MapCancelReservation` |
| `src/VenuePass.Api/appsettings.Development.json` | Add `"Ticketing"` section |

### New Test Files

| File | Covers |
|------|--------|
| `Ticketing/Reservations/CreateReservationTests.cs` | E1 (happy path), E2 (rejections) |
| `Ticketing/Reservations/GetReservationTests.cs` | B2 read scenarios |
| `Ticketing/Reservations/CancelReservationTests.cs` | B3, E3 (inventory released on cancel) |
| `Ticketing/Reservations/ExpireReservationTests.cs` | B4/B5 (expire handler releases inventory) |
| `Ticketing/Reservations/ReservationConcurrencyTests.cs` | E5 (double-reservation, same seat/pool) |

### Modified Test Files

| File | Change |
|------|--------|
| `Ticketing/Fixtures/TicketingSeedHelpers.cs` | Add `CreateReservationAsync`, `GetInventorySeatIdsAsync`, `GetInventoryPoolIdsAsync`, `SetupActiveOfferAsync` |
| `Ticketing/Authorization/TicketingAuthorizationTests.cs` | Add 3 auth tests for new endpoints (E7) |

## Acceptance Criteria

### CreateReservation (B1)

- [ ] `POST /reservations` with valid seat IDs against an active, on-sale offer returns `201` with `Status = Reserved`, correct `ExpiresAt`, item prices, and total
- [ ] `POST /reservations` with valid GA pool selection returns `201`
- [ ] `POST /reservations` with mixed seat and pool selections returns `201`
- [ ] After `CreateReservation`, `GET /events/{id}/inventory` shows reduced available seat/pool counts
- [ ] Request with inactive offer returns `409`
- [ ] Request with offer whose sale window has closed returns `409`
- [ ] Request with offer whose sale window has not yet opened returns `409`
- [ ] Request with seat not covered by any price zone returns `400`
- [ ] Request with unavailable seat returns `409`
- [ ] Request with GA quantity above availability returns `409`
- [ ] Request with duplicate seat IDs returns `400`
- [ ] Request with duplicate pool selections returns `400`
- [ ] Request with unknown offer ID returns `404`

### GetReservation (B2)

- [ ] `GET /reservations/{id}` returns reservation details including all items
- [ ] Unknown ID returns `404`

### CancelReservation (B3, B5)

- [ ] `DELETE /reservations/{id}` transitions `Reserved → Cancelled` and returns `204`
- [ ] Cancelled reservation releases seat availability
- [ ] Cancelled reservation restores GA pool available count
- [ ] Cancel on an already-cancelled reservation returns `409`
- [ ] Cancel on an expired reservation returns `409`

### ExpireReservation (B4, B5)

- [ ] Calling `ExpireReservationHandler` on a `Reserved` reservation past `ExpiresAt` transitions to `Expired` and releases inventory
- [ ] Expiration on a not-yet-expired reservation returns `Error.Conflict`
- [ ] Expiration on an already-cancelled reservation returns `Error.Conflict`

### Background Worker

- [ ] `ReservationExpirationWorker` is registered as a `BackgroundService`
- [ ] Worker is suppressed in integration tests via `RemoveAll<IHostedService>()`

### Concurrency (E5)

- [ ] Two parallel `CreateReservation` requests for the same seat result in exactly one `201` and one `409`
- [ ] Two parallel requests reserving the full GA pool capacity result in one success; total reserved does not exceed capacity

### Authorization (E7)

- [ ] Unauthenticated `POST /reservations` returns `401`
- [ ] Unauthenticated `GET /reservations/{id}` returns `401`
- [ ] Unauthenticated `DELETE /reservations/{id}` returns `401`

## Definition of Done

- [ ] All acceptance criteria met
- [ ] `dotnet build` passes at solution level
- [ ] `dotnet test` passes at solution level
- [ ] Architecture plan open questions resolved (see `docs/plans/milestone-04-capability-b.md` §Open Questions)
- [ ] Milestone-04 capability checkboxes for B1–B5 updated
