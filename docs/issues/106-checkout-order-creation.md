# Capability C: Checkout/order creation

## Summary

Convert a `Reserved` `Reservation` into a completed `Order` with issued `Ticket`s. Customers POST a checkout request containing buyer details; the system validates the reservation, creates an order from reservation price snapshots, marks reserved seats as sold, issues one ticket per reserved unit, and transitions the reservation to `Completed`. A `GetOrder` endpoint retrieves the order with its tickets. Checkout is idempotent: repeating the call on an already-completed reservation returns the existing order and tickets.

## Scope

- In scope:
  - `POST /reservations/{id}/checkout` — checkout a reserved reservation
  - `GET /orders/{orderId}` — retrieve an order with all items and tickets
  - `Order` aggregate with `Completed` status
  - `OrderItem` records copied from reservation price snapshots
  - `Ticket` issuance: one ticket per seat item, `Quantity` tickets per GA pool item
  - Unique, opaque ticket codes
  - `InventorySeat.Availability` transition `Reserved → Sold` on checkout
  - GA pool available count remains consumed after checkout (no release)
  - Idempotent checkout: repeated call on a completed reservation returns existing order/tickets
  - Integration tests (E4: checkout flow; E6: partial end-to-end)
  - Authorization tests (E7 for new endpoints)
- Out of scope:
  - Ticket retrieval by code (Capability D)
  - `GetTicketByCode` endpoint (Capability D)
  - Real payment provider integration
  - Order cancellation / refunds
  - Ticket check-in / admission lifecycle
  - Taxes, fees, discounts, promo codes

## Architecture Plan

Full implementation plan: [`docs/plans/milestone-04-capability-c.md`](../plans/milestone-04-capability-c.md)

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
  ],
  "tickets": [
    {
      "ticketId": "guid",
      "ticketCode": "string",
      "status": "Issued",
      "inventorySeatId": "guid",
      "generalAdmissionPoolId": null
    },
    {
      "ticketId": "guid",
      "ticketCode": "string",
      "status": "Issued",
      "inventorySeatId": null,
      "generalAdmissionPoolId": "guid"
    }
  ]
}
```

**Idempotent response `200 OK`:** Same shape as `201 Created`, returned when the reservation is already `Completed` and an order already exists.

### `GET /orders/{orderId}`

**Response `200 OK`:** Same shape as the checkout response.

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
8. Issue tickets:
   - One `Ticket` per `Seat` order item (set `InventorySeatId`).
   - `Quantity` tickets per `GeneralAdmissionPool` order item (set `GeneralAdmissionPoolId`).
   - Each ticket code is opaque, unique, and non-sequential.
9. Transition `Reservation.Status` → `Completed`.
10. Commit reservation, inventory, order, order items, and tickets in a **single `SaveChangesAsync`**.

SQL Server `rowversion` tokens on `reservations` and `inventory_seats` detect concurrent updates — the losing writer receives `DbUpdateConcurrencyException` → `Error.Concurrency` → HTTP 409.

## Idempotency Design

- `UNIQUE(orders.reservation_id)` enforces one order per reservation at the database level.
- `UNIQUE(tickets.ticket_code)` enforces globally unique ticket codes.
- If `CheckoutReservation` is called on a `Completed` reservation with an existing order, the handler loads and returns the existing order rather than creating a duplicate.

## Ticket Code Generation

Ticket codes are generated by the Ticketing module. Requirements:
- Opaque (must not encode order ID, seat ID, or other sensitive data).
- Unique across all tickets (enforced by database constraint).
- Non-sequential (must not allow enumeration).
- The specific generation strategy (GUID, CSPRNG base-32, etc.) is an implementation detail left to the developer.

## Files

### New Source Files

| File | Purpose |
|------|---------|
| `Features/CheckoutReservation/CheckoutReservationErrors.cs` | Typed error factory |
| `Features/CheckoutReservation/CheckoutReservationValidator.cs` | FluentValidation rules (`buyerName` required, `buyerEmail` required + format) |
| `Features/CheckoutReservation/CheckoutReservation.cs` | Handler, command, and result records |
| `Features/CheckoutReservation/CheckoutReservationEndpoint.cs` | `POST /reservations/{id}/checkout` |
| `Features/GetOrder/GetOrderErrors.cs` | Typed error factory |
| `Features/GetOrder/GetOrder.cs` | Handler, query, and result records |
| `Features/GetOrder/GetOrderEndpoint.cs` | `GET /orders/{id}` |

### Modified Source Files

| File | Change |
|------|--------|
| `ModuleConfiguration.cs` | Register `CheckoutReservationHandler`, `GetOrderHandler`; configure EF for `Order`, `OrderItem`, `Ticket` |
| `ModuleEndpointMappings.cs` | Wire `MapCheckoutReservation`, `MapGetOrder` |
| EF Core `DbContext` / configuration | Add `Order`, `OrderItem`, `Ticket` entity type configurations |
| EF Core migration | Add `orders`, `order_items`, `tickets` tables with uniqueness constraints and concurrency tokens |

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

## Acceptance Criteria

### CheckoutReservation (C1–C5)

- [ ] `POST /reservations/{id}/checkout` with a valid `Reserved`, non-expired reservation returns `201` with `Status = Completed`, correct order total, items, and tickets
- [ ] One seat reservation item produces exactly one ticket with `InventorySeatId` set and `GeneralAdmissionPoolId` null
- [ ] One GA pool reservation item produces `Quantity` tickets with `GeneralAdmissionPoolId` set and `InventorySeatId` null
- [ ] Mixed seat and pool reservation produces the correct combined ticket list
- [ ] After checkout, `GET /events/{id}/inventory` shows formerly reserved seats as sold
- [ ] After checkout, GA pool available count is unchanged (not restored)
- [ ] Checkout on a `Cancelled` reservation returns `409`
- [ ] Checkout on an `Expired` reservation returns `409`
- [ ] Checkout on a `Reserved` reservation past `ExpiresAt` returns `409`
- [ ] Concurrency conflict on reservation or inventory row returns `409`

### Idempotency (C5)

- [ ] `POST /reservations/{id}/checkout` on an already-completed reservation returns `200` with the existing order and tickets (no new rows created)
- [ ] `UNIQUE(orders.reservation_id)` constraint prevents duplicate orders at the database level
- [ ] `UNIQUE(tickets.ticket_code)` constraint is present

### GetOrder

- [ ] `GET /orders/{id}` returns order details including all items and all tickets
- [ ] Unknown order ID returns `404`

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
- [ ] E7 authorization tests for new endpoints passing
