# Capability A: Reservation Domain and Availability Locking

## Summary

Implement the `Reservation` aggregate with full status lifecycle, inventory availability locking for seats and GA pools, and price resolution from the active offer. Establish concurrency protection to prevent double booking and oversell.

## Scope

- In scope:
  - `Reservation` aggregate with status lifecycle (`Reserved`, `Completed`, `Cancelled`, `Expired`)
  - Inventory seat and GA pool reservation behavior (lock on reserve, release on cancel/expire, mark sold on checkout)
  - Price resolution: `Offer + target → PriceZone → UnitPrice` (server-side, single-axis model)
  - Price and currency snapshot stored on `ReservationItem` at creation time
  - Expiration timestamp set to `now + Ticketing:ReservationExpiryMinutes` (default 15 min)
  - Persistence: `reservations`, `reservation_items` tables with concurrency tokens
  - Concurrency tokens on `inventory_seats`, `inventory_pools`, and `reservations` rows
  - Update `GetInventoryStatus` seat-counting to respect `SeatAvailability` enum
- Out of scope:
  - `CreateReservation` endpoint and handler (Capability B)
  - Reservation expiration sweep worker (Capability B)
  - Checkout and order creation (Capability C)
  - Ticket issuance (Capability D)

## Domain Model

### Reservation

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `ReservationId` | |
| `OfferId` | `OfferId` | Reference to the active offer |
| `InventoryId` | `InventoryId` | Denormalized from offer for query convenience |
| `Status` | `ReservationStatus` | Starts as `Reserved` |
| `ExpiresAt` | `DateTimeOffset` | Set at creation; not nullable |
| `Currency` | `Currency` | Snapshotted from offer at creation |
| `Items` | `IReadOnlyList<ReservationItem>` | Owned collection |
| `Total` | `decimal` | Sum of item totals; derived and stored |

### ReservationStatus

```
Reserved → Completed
Reserved → Cancelled
Reserved → Expired
```

`Completed`, `Cancelled`, and `Expired` are terminal states. Invalid transitions are rejected with a domain rule violation.

### ReservationItem

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `ReservationItemId` | |
| `Type` | `ReservationItemType` | `Seat` or `GeneralAdmissionPool` |
| `InventorySeatId` | `InventorySeatId?` | Required for `Seat` items |
| `GeneralAdmissionPoolId` | `GeneralAdmissionPoolId?` | Required for `GeneralAdmissionPool` items |
| `PriceZoneId` | `PriceZoneId` | Snapshot reference — which zone resolved the price |
| `Quantity` | `int` | Always `1` for seat items; `> 0` for GA items |
| `UnitPrice` | `decimal` | Snapshotted from `PriceZone.Price` at reservation time |
| `Total` | `decimal` | `UnitPrice * Quantity` |

Rules:
- Seat item: `InventorySeatId` required, `GeneralAdmissionPoolId` null, `Quantity = 1`
- GA item: `GeneralAdmissionPoolId` required, `InventorySeatId` null, `Quantity > 0`

### Price Resolution

Given a target (seat ID or pool ID + quantity), resolve price as follows:
1. Load the active offer by `OfferId`
2. Find the `PriceZone` whose `InventorySeatItems` or `GeneralAdmissionPoolItems` contains the target ID
3. `UnitPrice = PriceZone.Price`, `Currency = Offer.Currency`
4. If no price zone covers the target → reject with error (target not covered by any price zone)

### Inventory Mutations

| Event | Seat | GA Pool |
|-------|------|---------|
| Reserve | `Availability → Reserved` | `AvailableCount -= quantity` |
| Cancel / Expire | `Availability → Available` | `AvailableCount += quantity` |
| Checkout (complete) | `Availability → Sold` | no change (already consumed) |

`GetInventoryStatus` seat-counting must filter by `Availability == Available` (currently counts all seats — valid in M03 since no reservations existed).

## Persistence

New tables:

| Table | Key Columns |
|-------|------------|
| `reservations` | `id`, `offer_id`, `inventory_id`, `status`, `expires_at`, `currency`, `total`, `row_version` |
| `reservation_items` | `id`, `reservation_id`, `type`, `inventory_seat_id`, `general_admission_pool_id`, `price_zone_id`, `quantity`, `unit_price`, `total` |

Concurrency tokens added to existing tables:

| Table | Column |
|-------|--------|
| `inventory_seats` | `row_version` |
| `inventory_pools` | `row_version` |
| `reservations` | `row_version` |

Uniqueness constraints: none required at this capability level (order idempotency is Capability C).

## Acceptance Criteria

- [ ] `Reservation` aggregate can be created in `Reserved` state with valid items and expiration timestamp
- [ ] `Reservation.Cancel()` transitions `Reserved → Cancelled`; rejects terminal states
- [ ] `Reservation.Expire()` transitions `Reserved → Expired`; rejects terminal states
- [ ] `Reservation.Complete()` transitions `Reserved → Completed`; rejects terminal states
- [ ] Price resolution finds the correct `PriceZone` for a given seat ID or pool ID
- [ ] Price resolution rejects a target not covered by any price zone
- [ ] `Inventory.ReserveSeats()` transitions seats `Available → Reserved`; rejects unavailable seats
- [ ] `Inventory.ReservePoolQuantity()` decrements `AvailableCount`; rejects quantity above availability
- [ ] `Inventory.ReleaseSeats()` transitions seats `Reserved → Available`
- [ ] `Inventory.ReleasePoolQuantity()` increments `AvailableCount`
- [ ] `Inventory.SellSeats()` transitions seats `Reserved → Sold`
- [ ] `GetInventoryStatus` returns correct available seat counts when seats are in `Reserved` or `Sold` state
- [ ] Concurrency tokens are present on `inventory_seats`, `inventory_pools`, and `reservations`
- [ ] Domain unit tests cover all status transitions and price resolution

## Vertical Slices

- [ ] A1: Implement `Reservation` aggregate with status lifecycle and `ReservationItem` owned collection
- [ ] A2: Add `ReserveSeats`, `ReleaseSeats`, `SellSeats`, `ReservePoolQuantity`, `ReleasePoolQuantity` to `Inventory`; fix `GetInventoryStatus` seat counting
- [ ] A3: Implement price resolution logic (`Offer + target → PriceZone → UnitPrice`)
- [ ] A4: Add EF Core configuration and migrations for `reservations`, `reservation_items`; add concurrency tokens to `inventory_seats` and `inventory_pools`
- [ ] A5: Unit tests covering reservation status transitions, inventory mutations, and price resolution

## Risks and Assumptions

- Concurrency token migrations on `inventory_seats` and `inventory_pools` alter existing tables — verify no data loss on existing test data
- Price resolution traverses offer price zones in memory; acceptable for M04 scale (no SQL join required)
- `GetInventoryStatus` fix is a one-liner but must be verified by existing integration tests

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Unit tests passing
- [ ] `dotnet build` passes at solution level
- [ ] `dotnet test` passes at solution level
- [ ] Docs updated if behavior changed
