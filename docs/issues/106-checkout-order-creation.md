# Capability C: Checkout/order creation

## Summary

Convert a `Reserved` `Reservation` into a completed `Order`. Customers POST a checkout request containing buyer details; the system validates the reservation, creates an order from reservation price snapshots, marks reserved seats as sold, and transitions the reservation to `Completed`. Checkout is idempotent: repeating the call on an already-completed reservation returns the existing order. Ticket issuance and order retrieval are delivered in Capability D.

## Scope

- In scope:
  - `POST /reservations/{id}/checkout` — checkout a reserved reservation
  - `GET /orders/{orderId}` — retrieve an order with all items (tickets field added in Capability D)
  - `Order` aggregate with `Completed` status
  - `OrderItem` records copied from reservation price snapshots
  - `InventorySeat.Availability` transition `Reserved → Sold` on checkout
  - GA pool available count remains consumed after checkout (no release)
  - Idempotent checkout: repeated call on a completed reservation returns the existing order
  - Integration tests (E4: checkout flow)
  - Authorization tests (E7 for new endpoints)
- Out of scope:
  - Ticket issuance (Capability D)
  - `tickets` field on `GetOrder` response (Capability D extends this endpoint)
  - `GetTicketByCode` endpoint (Capability D)
  - Real payment provider integration
  - Order cancellation / refunds
  - Ticket check-in / admission lifecycle
  - Taxes, fees, discounts, promo codes

## Architecture Plan

Full implementation plan: [`docs/plans/milestone-04-capability-c.md`](../plans/milestone-04-capability-c.md)

## Domain Model

### Order

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `OrderId` | |
| `ReservationId` | `ReservationId` | FK; unique — one reservation produces at most one order |
| `OfferId` | `OfferId` | Denormalized from reservation for query convenience |
| `InventoryId` | `InventoryId` | Denormalized from reservation for query convenience |
| `BuyerName` | `string` | Captured at checkout time |
| `BuyerEmail` | `string` | Captured at checkout time |
| `Currency` | `Currency` | Snapshotted from reservation currency |
| `Total` | `decimal` | Sum of order item totals; derived and stored |
| `Status` | `OrderStatus` | Starts as `Completed` (no pending state in M04) |
| `Items` | `IReadOnlyList<OrderItem>` | Owned collection, copied from reservation items |

`Order` has no status transitions in M04. It is created in `Completed` state and remains there. `Cancelled` and `Refunded` are anticipated future states.

### OrderItem

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `OrderItemId` | |
| `Type` | `OrderItemType` | `Seat` or `GeneralAdmissionPool` |
| `InventorySeatId` | `InventorySeatId?` | Required for `Seat` items |
| `GeneralAdmissionPoolId` | `GeneralAdmissionPoolId?` | Required for `GeneralAdmissionPool` items |
| `PriceZoneId` | `PriceZoneId` | Preserved price snapshot reference |
| `Quantity` | `int` | Always `1` for seat items; `> 0` for GA items |
| `UnitPrice` | `decimal` | Copied from `ReservationItem.UnitPrice` |
| `Total` | `decimal` | `UnitPrice * Quantity` |

Rules mirror `ReservationItem`: seat items require `InventorySeatId` and `Quantity = 1`; GA items require `GeneralAdmissionPoolId` and `Quantity > 0`.

### OrderStatus

```
Completed   (only status in M04)
```

`Cancelled` and `Refunded` are anticipated future states pending order-cancellation and refund scope.

### Inventory Mutation on Checkout

`Inventory.SellSeats(seatIds)` was defined in Capability A but is invoked for the first time here:

| Item type | Mutation |
|-----------|---------|
| `Seat` | `InventorySeat.Availability → Sold` |
| `GeneralAdmissionPool` | No change — quantity was already consumed from `AvailableCount` at reservation time |

## Persistence

New tables:

| Table | Key columns |
|-------|------------|
| `orders` | `id`, `reservation_id`, `offer_id`, `inventory_id`, `buyer_name`, `buyer_email`, `currency`, `total`, `status` |
| `order_items` | `id`, `order_id`, `type`, `inventory_seat_id`, `general_admission_pool_id`, `price_zone_id`, `quantity`, `unit_price`, `total` |

Required uniqueness constraints:

| Table | Constraint |
|-------|-----------|
| `orders` | `UNIQUE(reservation_id)` |

No new concurrency tokens required — `reservations` and `inventory_seats` tokens from Capability A cover the checkout race.

## Endpoints

| Method | Path | Auth | Success |
|--------|------|------|---------|
| `POST` | `/reservations/{reservationId:guid}/checkout` | Any authenticated role | `201 Created` (new order) / `200 OK` (idempotent) |
| `GET` | `/orders/{orderId:guid}` | Any authenticated role | `200 OK` |

## Request and Response Contracts

### `POST /reservations/{reservationId}/checkout`

**Request body:**

```json
{
  "buyerName": "string",
  "buyerEmail": "string"
}
```

**Response `201 Created`:**

```json
{
  "orderId": "guid",
  "reservationId": "guid",
  "status": "Completed",
  "currency": "USD",
  "total": 175.00,
  "buyerName": "string",
  "buyerEmail": "string",
  "items": [
    {
      "orderItemId": "guid",
      "type": "Seat",
      "inventorySeatId": "guid",
      "generalAdmissionPoolId": null,
      "priceZoneId": "guid",
      "quantity": 1,
      "unitPrice": 50.00,
      "total": 50.00
    },
    {
      "orderItemId": "guid",
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

**Idempotent response `200 OK`:** Same shape as `201 Created`, returned when the reservation is already `Completed` and an order already exists.

### `GET /orders/{orderId}`

**Response `200 OK`:**

```json
{
  "orderId": "guid",
  "reservationId": "guid",
  "status": "Completed",
  "currency": "USD",
  "total": 175.00,
  "buyerName": "string",
  "buyerEmail": "string",
  "items": [
    {
      "orderItemId": "guid",
      "type": "Seat",
      "inventorySeatId": "guid",
      "generalAdmissionPoolId": null,
      "priceZoneId": "guid",
      "quantity": 1,
      "unitPrice": 50.00,
      "total": 50.00
    }
  ]
}
```

> Capability D extends this response to add a `tickets` array once ticket issuance is implemented.

## Error Paths

| Scenario | HTTP Status |
|----------|-------------|
| Reservation not found | 404 |
| Order not found | 404 |
| Reservation is `Cancelled` | 409 |
| Reservation is `Expired` | 409 |
| Reservation is `Reserved` but past `ExpiresAt` | 409 |
| Concurrency conflict on reservation or inventory row | 409 |

## Checkout Flow

1. Load `Reservation` (tracked). If status is `Completed` and an order exists → return existing order (idempotent, `200 OK`).
2. Validate status is `Reserved` and `ExpiresAt` is in the future.
3. Load `Inventory` (tracked) for seat availability transitions.
4. Create `Order` with `BuyerName`, `BuyerEmail`, currency, and total from the reservation.
5. Copy each `ReservationItem` into an `OrderItem`, preserving the price snapshot.
6. For each `Seat` item: transition `InventorySeat.Availability` → `Sold`.
7. For each `GeneralAdmissionPool` item: no pool count change (availability was already decremented at reservation time; GA pool quantity remains consumed).
8. Transition `Reservation.Status` → `Completed`.
9. Commit reservation, inventory, order, and order items in a **single `SaveChangesAsync`**.

SQL Server `rowversion` tokens on `reservations` and `inventory_seats` detect concurrent updates — the losing writer receives `DbUpdateConcurrencyException` → `Error.Concurrency` → HTTP 409.

## Idempotency Design

- `UNIQUE(orders.reservation_id)` enforces one order per reservation at the database level.
- If `CheckoutReservation` is called on a `Completed` reservation with an existing order, the handler loads and returns the existing order rather than creating a duplicate.

## Files

### New Source Files

#### Domain

| File | Purpose |
|------|---------|
| `Domain/Order.cs` | `Order` aggregate, `OrderItem` owned entity, `OrderStatus` enum |

#### Infrastructure / Persistence

| File | Purpose |
|------|---------|
| `Infrastructure/Persistence/OrderConfiguration.cs` | EF Core entity type configuration for `Order` and `OrderItem`; unique index on `reservation_id` |
| EF Core migration | `orders`, `order_items` tables; `UNIQUE(orders.reservation_id)` |

#### Features

| File | Purpose |
|------|---------|
| `Features/CheckoutReservation/CheckoutReservationErrors.cs` | Typed error factory |
| `Features/CheckoutReservation/CheckoutReservationValidator.cs` | FluentValidation rules (`buyerName` required, `buyerEmail` required + valid email format) |
| `Features/CheckoutReservation/CheckoutReservation.cs` | Handler, command, and result records |
| `Features/CheckoutReservation/CheckoutReservationEndpoint.cs` | `POST /reservations/{id}/checkout` |
| `Features/GetOrder/GetOrderErrors.cs` | Typed error factory |
| `Features/GetOrder/GetOrder.cs` | Handler, query, and result records (no tickets field) |
| `Features/GetOrder/GetOrderEndpoint.cs` | `GET /orders/{id}` |

### Modified Source Files

| File | Change |
|------|--------|
| `ModuleConfiguration.cs` | Register `CheckoutReservationHandler`, `GetOrderHandler`; register `Order` EF configuration |
| `ModuleEndpointMappings.cs` | Wire `MapCheckoutReservation`, `MapGetOrder` |

### New Test Files

| File | Covers |
|------|--------|
| `Ticketing/Orders/CheckoutReservationTests.cs` | C2–C5, E4: happy path (seat, GA, mixed), idempotent checkout, rejection cases (cancelled, expired, past expiry) |
| `Ticketing/Orders/GetOrderTests.cs` | Order retrieval, unknown ID |

### Modified Test Files

| File | Change |
|------|--------|
| `Ticketing/Fixtures/TicketingSeedHelpers.cs` | Add `CheckoutReservationAsync`, `GetOrderAsync` |
| `Ticketing/Authorization/TicketingAuthorizationTests.cs` | Add 2 auth tests for new endpoints (E7) |

## Vertical Slices

- [ ] C1: Implement `Order` aggregate, `OrderItem` owned entity, and `OrderStatus` enum
- [ ] C2: Add EF Core configuration and migration for `orders`, `order_items` with `UNIQUE(reservation_id)`
- [ ] C3: Implement `CheckoutReservationHandler` with inventory sell mutation and idempotency path
- [ ] C4: Deliver `POST /reservations/{id}/checkout` endpoint
- [ ] C5: Deliver `GET /orders/{id}` endpoint and handler (without tickets field)
- [ ] C6: Unit tests for `Order` creation rules; integration tests for E4 checkout scenarios

## Acceptance Criteria

### CheckoutReservation (C1–C5)

- [ ] `POST /reservations/{id}/checkout` with a valid `Reserved`, non-expired reservation returns `201` with `Status = Completed`, correct order total, and items
- [ ] After checkout, `GET /events/{id}/inventory` shows formerly reserved seats as sold
- [ ] After checkout, GA pool available count is unchanged (not restored)
- [ ] Checkout on a `Cancelled` reservation returns `409`
- [ ] Checkout on an `Expired` reservation returns `409`
- [ ] Checkout on a `Reserved` reservation past `ExpiresAt` returns `409`
- [ ] Concurrency conflict on reservation or inventory row returns `409`

### GetOrder

- [ ] `GET /orders/{id}` returns order details including all items
- [ ] Unknown order ID returns `404`

### Idempotency (C5)

- [ ] `POST /reservations/{id}/checkout` on an already-completed reservation returns `200` with the existing order (no new rows created)
- [ ] `UNIQUE(orders.reservation_id)` constraint prevents duplicate orders at the database level

### Inventory State After Checkout (C4)

- [ ] Reserved seats transition to `Sold` after checkout; they cannot be reserved again
- [ ] GA pool available count is not restored after checkout

### Authorization (E7)

- [ ] Unauthenticated `POST /reservations/{id}/checkout` returns `401`
- [ ] Unauthenticated `GET /orders/{id}` returns `401`

## Definition of Done

- [ ] All acceptance criteria met
- [ ] `dotnet build` passes at solution level
- [ ] `dotnet test` passes at solution level
- [ ] Milestone-04 capability checkboxes for C1–C5 updated
- [ ] E4 integration test scenario implemented and passing
- [ ] E7 authorization tests for both new endpoints passing
