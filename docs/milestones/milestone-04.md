# Milestone 04 — Ticketing: Reservation, Orders & Ticket Issuance

## Milestone Outcome

Reservation and checkout lifecycle is implemented: customers can reserve available inventory from an active offer, complete simulated checkout, and receive issued tickets.

## In Scope

- [x] Capability A: Reservation domain and availability locking
- [x] Capability B: Reservation API and expiration flow
- [x] Capability C: Checkout/order creation
- [x] Capability D: Ticket issuance and ticket retrieval
- [ ] Capability E: Integration and concurrency tests

## Capability Breakdown

### Capability A: Reservation domain and availability locking

- [x] A1: Implement `Reservation` aggregate with status lifecycle
- [x] A2: Add inventory reservation behavior for seats and GA pools; update `GetInventoryStatus` seat-counting to respect `SeatAvailability` enum (available vs reserved vs sold)
- [x] A3: Resolve price from active `Offer + target -> PriceZone -> Price` using current single-axis pricing model
- [x] A4: Persist reservations and reservation items
- [x] A5: Add concurrency protection to prevent double booking/oversell

### Capability B: Reservation API and expiration flow

- [x] B1: Deliver `CreateReservation` endpoint and handler
- [x] B2: Deliver `GetReservation` endpoint and handler
- [x] B3: Deliver `CancelReservation` endpoint and handler
- [x] B4: Deliver reservation expiration command/handler path and background sweep worker
- [x] B5: Ensure cancelled/expired reservations release inventory

### Capability C: Checkout/order creation

- [x] C1: Implement `Order` aggregate
- [x] C2: Deliver `CheckoutReservation` endpoint and handler
- [x] C3: Persist order and order items with price snapshots
- [x] C4: Mark reserved inventory as sold after successful checkout
- [x] C5: Ensure one reservation can produce at most one order

### Capability D: Ticket issuance and ticket retrieval

- [x] D1: Implement ticket issuance after successful order creation
- [x] D2: Generate unique ticket codes suitable for future check-in
- [x] D3: Persist issued tickets
- [x] D4: Deliver `GetOrder` endpoint including issued tickets
- [x] D5: Deliver ticket lookup endpoint by ticket code

### Capability E: Integration and concurrency tests

- [ ] E1: Reservation flow tests: active offer → reserve seats/pools
- [ ] E2: Reservation rejection tests: unavailable target, inactive offer, expired sale window
- [ ] E3: Expiration/cancellation tests release inventory
- [ ] E4: Checkout tests create order, mark inventory sold, and issue tickets
- [ ] E5: Double-reservation/concurrency tests prevent oversell
- [ ] E6: End-to-end test: publish event → inventory → offer → reservation → order → tickets
- [ ] E7: Authorization enforcement tests across all new M04 Ticketing endpoints

## Functional Requirements Baseline (M04)

These requirements define minimum business behavior for M04 and should be treated as implementation gates.

### Endpoint Authorization Requirements

- [x] All reservation, checkout, and ticket endpoints require authentication (any authenticated role).
- [x] No EventManager restriction applies to customer-facing endpoints (`CreateReservation`, `GetReservation`, `CancelReservation`, `CheckoutReservation`, `GetOrder`, `GetTicketByCode`).

### Reservation Requirements

- [x] A reservation can be created only against an active offer.
- [x] Offer sale window is enforced at reservation time.
- [x] Reservation targets must belong to the offer’s inventory.
- [x] Reservation targets must be covered by a configured price zone.
- [x] Reserved seats must currently be available.
- [x] Reserved GA pool quantity must not exceed available count.
- [x] Reservation is atomic: if any requested target cannot be reserved, nothing is reserved.
- [x] Reservation stores a price snapshot from the resolved price zone.
- [x] Reservation currency is taken from the active Offer at reservation creation time.
- [x] Reservation receives an expiration timestamp set to `now + ReservationExpiryMinutes` (default 15 minutes, configurable via `Ticketing:ReservationExpiryMinutes`; sweep cadence visibility is provided via `Ticketing:ExpirationSweepInterval` in development config).
- [x] Reservation status lifecycle includes at minimum: `Reserved`, `Completed`, `Cancelled`, `Expired`.
- [x] Only `Reserved` reservations may transition state.
- [x] Valid transitions are only: `Reserved -> Completed`, `Reserved -> Cancelled`, `Reserved -> Expired`.
- [x] `Completed`, `Cancelled`, and `Expired` are terminal states.
- [x] Reserved reservations reduce available inventory.
- [x] Cancelled or expired reservations release inventory.
- [x] Completed reservations do not release inventory.
- [x] `CreateReservation` rejects duplicate seat IDs and duplicate GA pool selections in the same request.

### Checkout / Order Requirements

- [x] A reserved, non-expired reservation can be checked out.
- [x] Checkout creates exactly one order for the reservation.
- [x] Checkout is idempotent enough to prevent duplicate orders/tickets for the same reservation.
- [x] `CheckoutReservation` on an already completed reservation returns the existing order/tickets if an order exists.
- [x] `CheckoutReservation` rejects cancelled or expired reservations.
- [x] Order total is calculated from reservation price snapshots.
- [x] No external payment provider is integrated in M04; checkout represents a successful simulated payment.
- [x] After checkout, reservation becomes `Completed`.
- [x] After checkout, reserved seats become sold.
- [x] After checkout, reserved GA quantity remains consumed from pool availability.

### Ticket Issuance Requirements

- [x] Successful checkout issues tickets.
- [x] One reserved seat produces one ticket.
- [x] Each reserved GA unit produces one ticket.
- [x] Each ticket has a unique ticket code.
- [x] Tickets are persisted and retrievable by order.
- [x] Ticket status starts as `Issued`.
- [x] Ticket check-in/use is deferred to Milestone 05.

### Inventory Status Requirements

- [x] Inventory status reflects reserved reservations and completed purchases.
- [x] Available seat count excludes reserved and sold seats.
- [x] Available GA pool count excludes reserved and sold quantity.
- [x] Cancelled/expired reservations restore availability.

### API Contract Requirements

- [x] `CreateReservation` request includes: offer ID, seat IDs, GA pool selections with quantity.
- [x] Reservation route shape is top-level: `POST /reservations`, `GET /reservations/{id}`, `DELETE /reservations/{id}`; `offerId` stays in create request body.
- [x] `CreateReservation` response includes: reservation ID, status, expiration time, items, prices, and total.
- [x] `GetReservation` response includes: status, expiration time, items, prices, and total.
- [x] `CancelReservation` cancels only reserved reservations.
- [x] `CheckoutReservation` request includes: reservation ID and buyer/contact details.
- [x] `CheckoutReservation` response includes: order ID, order total, and issued tickets.
- [x] `GetOrder` response includes: order status, items, total, and tickets.
- [x] `GetTicketByCode` endpoint is `GET /tickets/{ticketCode}` and requires authentication.
- [x] `GetTicketByCode` response includes: ticket code, ticket status, order ID, and related inventory target reference.
- [x] `GetTicketByCode` returns `404` when the ticket code does not exist.

## Accepted Decisions (Locked For M04)

1. Reservations, orders, and tickets remain inside the Ticketing module for M04.
2. No real payment integration in M04; checkout simulates successful payment.
3. No Identity dependency in M04; buyer payload is minimal (`name` + `email`) and passed explicitly in checkout request.
4. Inventory remains the source of availability.
5. Reservation creation mutates inventory availability in the same transaction.
6. Reservation stores price snapshots so later pricing changes do not affect reserved reservations/orders.
7. Checkout creates one order per reservation.
8. Ticket codes are generated by Ticketing and will become the input for Attendance check-in in M05.
9. Ticket check-in state is not handled in Ticketing during M04.
10. Advanced pricing remains out of scope; price resolution uses current single-axis model: `Offer + target -> PriceZone -> Price`.
11. Reservation expiration uses a required command/handler path plus a required background sweep worker in M04.
12. No partial reservation success: all requested items reserve successfully or the command fails.
13. Checkout is idempotent: if a reservation has already produced an order, repeated checkout returns the existing order and tickets.
14. Seat inventory uses explicit availability states: `Available`, `Reserved`, `Sold`.
15. GA pool inventory remains quantity-based; reserved quantity is removed from `AvailableCount` and restored only on cancellation/expiration.
16. Price resolution is server-side: `Offer + target -> PriceZone -> Price`. Clients do not select price zones during reservation.
17. Order totals are derived only from reservation price snapshots; no taxes, fees, discounts, or coupons in M04.
18. Ticket lookup by code is required in M04; `GetOrder` with issued tickets is also mandatory.
19. Ticketing does not emit new outbound integration events in M04 unless a concrete consumer requires them.
20. Concurrency strategy in M04:
    - Reservation commands run in a single database transaction.
    - Inventory seat/pool rows use optimistic concurrency tokens.
    - Reservation row uses optimistic concurrency token to protect checkout vs expiration/cancellation races.
    - Database constraints protect idempotency where possible.
21. Reservation transition rules are explicit and strict:
    - Valid transitions: `Reserved -> Completed`, `Reserved -> Cancelled`, `Reserved -> Expired`.
    - Terminal states: `Completed`, `Cancelled`, `Expired`.
    - Invalid transitions are rejected (for example: `Completed` cannot be cancelled or expired; `Cancelled`/`Expired` cannot be checked out).
    - In checkout vs expiration/cancellation races, the first committed transition wins; later conflicting attempts return a conflict/state error.
22. Error mapping uses conflict semantics consistently for state/contention scenarios (for example unavailable inventory, invalid transition state, and race losers). Availability/state/contention failures are represented with `DomainConflictException` and map to HTTP 409.
23. `ExpireReservation` remains an internal command/handler path in M04. No public HTTP endpoint is exposed for explicit expiration.
24. Background sweep complexity is intentionally limited in M04: no advanced retry/backoff strategy beyond basic logging and next run.
25. Serial expiration sweep processing is acceptable for M04. Parallelization/bulk processing is deferred; bounded batch processing is allowed.
26. Reservation expiration window defaults to 15 minutes and is configurable via `Ticketing:ReservationExpiryMinutes` in app configuration.
27. `Ticketing:ExpirationSweepInterval` is surfaced in `appsettings.Development.json` for visibility, while code defaults and options validation remain in place.
28. All reservation, checkout, and ticket endpoints require authentication (any authenticated role). No EventManager restriction applies to customer-facing endpoints.
29. `OrderStatus` contains only `Completed` for M04. `Cancelled` and `Refunded` are anticipated future states pending order-cancellation and refund scope. `Pending` is deferred until real async payment integration is introduced.
30. Offer sale window is enforced only when creating a reservation. Checkout may complete an existing non-expired reservation even if the sale window has closed.
31. Expiration is enforced by time checks in commands as well as by the background sweep. A `Reserved` reservation past `ExpiresAt` is treated as expired and cannot be checked out.
32. Ticket codes are opaque, unique, and non-sequential; they must not encode sensitive/order data.

## Proposed Domain Model

### Reservation

- Id
- OfferId
- InventoryId
- Status
- ExpiresAt
- Currency
- Items
- Total

### Reservation Status

- Reserved
- Completed
- Cancelled
- Expired

Valid transitions:

- `Reserved -> Completed`
- `Reserved -> Cancelled`
- `Reserved -> Expired`

Terminal states:

- `Completed`
- `Cancelled`
- `Expired`

---

### Reservation Item

A reservation item represents either one reserved seat or a reserved GA pool quantity.

- Id
- Type (`Seat` / `GeneralAdmissionPool`)
- InventorySeatId?
- GeneralAdmissionPoolId?
- PriceZoneId
- Quantity
- UnitPrice
- Total

Rules:

- For `Seat` items:
  - `InventorySeatId` is required
  - `GeneralAdmissionPoolId` is null
  - `Quantity` must be `1`

- For `GeneralAdmissionPool` items:
  - `GeneralAdmissionPoolId` is required
  - `InventorySeatId` is null
  - `Quantity` must be greater than `0`

- `UnitPrice` is snapshotted from the resolved `PriceZone`.
- `Total = UnitPrice * Quantity`.
- Item amounts use `Reservation.Currency`.

---

### Order

Represents a completed simulated checkout created from one reservation.

- Id
- ReservationId
- OfferId
- InventoryId
- BuyerName
- BuyerEmail
- Currency
- Total
- Status
- Items
- Tickets

### Order Status

For M04:

- Completed

Notes:

- No pending/payment-failed states in M04. Checkout represents a synchronous simulated payment — an order only exists after successful completion.
- One reservation can produce at most one order.
- Order currency comes from the reservation currency (which was taken from the Offer).
- `Cancelled` and `Refunded` are anticipated future states. `Pending` is deferred until real async payment integration is introduced.

---

### Order Item

Order items are copied from reservation item price snapshots.

- Id
- Type (`Seat` / `GeneralAdmissionPool`)
- InventorySeatId?
- GeneralAdmissionPoolId?
- PriceZoneId
- Quantity
- UnitPrice
- Total

Rules:

- For `Seat` items:
  - `InventorySeatId` is required
  - `GeneralAdmissionPoolId` is null
  - `Quantity` must be `1`

- For `GeneralAdmissionPool` items:
  - `GeneralAdmissionPoolId` is required
  - `InventorySeatId` is null
  - `Quantity` must be greater than `0`

- `Total = UnitPrice * Quantity`.
- Item amounts use `Order.Currency`.

---

### Ticket

Represents an issued ticket produced by successful checkout.

- Id
- OrderId
- OrderItemId
- TicketCode
- InventorySeatId?
- GeneralAdmissionPoolId?
- Status

### Ticket Status

For M04:

- Issued

Rules:

- One seat order item produces one ticket.
- One GA order item produces `Quantity` tickets.
- Seat ticket has `InventorySeatId`.
- GA ticket has `GeneralAdmissionPoolId`.
- Ticket code is opaque, unique, and non-sequential.
- Ticket check-in/use is deferred to M05.

## Persistence Expectations

Required tables/constraints:

- `reservations`
- `reservation_items`
- `orders`
- `order_items`
- `tickets`

Required uniqueness:

- `UNIQUE(orders.reservation_id)`
- `UNIQUE(tickets.ticket_code)`

Transaction and idempotency expectations:

- Reservation-changing commands execute in a single database transaction.
- Database constraints are used where possible to guard idempotency.

Required concurrency tokens:

- `reservations.row_version`
- `inventory_seats.row_version`
- `inventory_pools.row_version`

## Slice Start Gate

- [ ] Functional requirements above reviewed and accepted.
- [ ] Reservation lifecycle and state transition rules approved.
- [x] Concurrency strategy selected and documented.

## Out of Scope

- Real payment provider integration
- Refunds
- Order cancellation after successful checkout
- Ticket transfer
- Ticket voiding
- Ticket check-in/admission lifecycle
- Email delivery of tickets
- User accounts and authentication changes
- Promo codes, discounts, taxes, fees
- Two-axis pricing / PriceType
- Dynamic pricing
- Seat map UI
- Production-grade distributed scheduling, advanced retry/backoff, and operational monitoring for reservation expiration

## Definition of Done

- [ ] All in-scope capability issues are implemented and merged.
- [ ] Reservation lifecycle is validated end-to-end (`Reserved` -> `Completed`/`Cancelled`/`Expired`).
- [ ] Concurrency tests demonstrate no double booking/oversell for seats and GA pools.
- [ ] Checkout creates exactly one order per reservation and issues tickets.
- [ ] Inventory status reflects reserved, released, and sold states correctly.
- [ ] Architecture tests pass without new module-boundary violations.
- [ ] Baseline CI remains green.
- [ ] Milestone and issue docs are updated to reflect completion state.

## Validation Checklist

- [ ] `dotnet build` passes at solution level.
- [ ] `dotnet test` passes at solution level.
- [ ] `CreateReservation` succeeds for available priced seats.
- [ ] `CreateReservation` succeeds for available priced GA pools.
- [ ] `CreateReservation` rejects unavailable seats.
- [ ] `CreateReservation` rejects GA quantity above availability.
- [ ] `CreateReservation` rejects inactive offers.
- [ ] `CreateReservation` rejects targets not covered by price zones.
- [ ] `CreateReservation` rejects offer whose sale window has ended.
- [ ] `CancelReservation` releases inventory.
- [ ] `ExpireReservation` releases inventory.
- [ ] Background sweep expires overdue reserved reservations via the same expiration command path.
- [ ] `CheckoutReservation` creates an order.
- [ ] `CheckoutReservation` issues tickets.
- [ ] `CheckoutReservation` marks seat inventory as sold.
- [ ] `CreateReservation` rejects offer whose sale window has not started.
- [ ] `CheckoutReservation` returns existing order/tickets when repeated for an already completed reservation.
- [ ] `ExpireReservation` racing with checkout cannot both release and sell the same inventory.
- [ ] `CancelReservation` rejects reservations in terminal states.
- [ ] `ExpireReservation` rejects reservations in terminal states.
- [ ] `CheckoutReservation` rejects cancelled reservations.
- [ ] `CheckoutReservation` rejects expired reservations.
- [ ] In checkout vs expire/cancel races, first committed transition wins and the other attempt returns a conflict/state error.
- [ ] `GetTicketByCode` returns issued ticket details for an existing ticket code.
- [ ] Ticket codes are unique at database level.
- [ ] Duplicate checkout does not create duplicate order/tickets.
- [ ] Inventory status reflects reserved and sold inventory.
- [ ] Authorization enforcement is verified on new Ticketing endpoints.

## Risks and Dependencies

- Concurrency correctness for seat and GA reservations is the highest technical risk in M04.
- Reservation expiration flow must remain deterministic in tests.
- Checkout idempotency must be explicit to avoid duplicate orders/tickets under retries.
- M04 depends on M03 pricing invariants (single-axis model and target-to-zone mapping) staying stable.
- Integration test infrastructure must support race-condition/concurrency scenarios reliably in CI.
