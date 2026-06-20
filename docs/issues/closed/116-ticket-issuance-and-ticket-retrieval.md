# Capability D: Ticket issuance and ticket retrieval

## Summary

Issue tickets as part of successful checkout and expose read APIs for orders (including tickets) and ticket lookup by code. Each seat reservation item yields one ticket; each GA reservation unit yields one ticket. Ticket codes are opaque, unique, and non-sequential so they can be used safely for future attendance check-in flows.

## Scope

- In scope:
  - Ticket issuance during `CheckoutReservation`
  - Persisting `Ticket` aggregates linked to `Order` and `OrderItem`
  - Unique ticket code generation (`UNIQUE(tickets.ticket_code)`)
  - Extend `GET /orders/{orderId}` to include issued tickets
  - Add `GET /tickets/{ticketCode}` lookup endpoint
  - Integration tests (E4 extension: checkout issues tickets)
  - Authorization tests (E7 for new ticket endpoints)
- Out of scope:
  - Ticket check-in / admission lifecycle (Milestone 05)
  - Ticket transfer, voiding, reissuance
  - Barcode/QR rendering and delivery channels (email/SMS/passbook)
  - Payment provider integration, refunds, order cancellation

## Domain Model

### Ticket

`Ticket` is modeled as an aggregate root in M04. While behavior is intentionally minimal (`Issued` only), it has independent identity (`TicketCode`), independent lookup access (`GET /tickets/{ticketCode}`), and a future lifecycle owned by Ticketing/Attendance flows.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `TicketId` | |
| `OrderId` | `OrderId` | FK |
| `OrderItemId` | `OrderItemId` | stored as scalar reference |
| `TicketCode` | `TicketCode` | Normalized, opaque, unique, non-sequential |
| `InventorySeatId` | `InventorySeatId?` | Required for seat-derived tickets |
| `GeneralAdmissionPoolId` | `GeneralAdmissionPoolId?` | Required for GA-derived tickets |
| `Status` | `TicketStatus` | Starts as `Issued` |
| `CreatedAt` | `DateTimeOffset` | UTC issuance timestamp |

Aggregate boundary notes:

- TicketCode format and presence are enforced by the `Ticket` aggregate. Global uniqueness is enforced by the ticket issuance service and the database unique constraint.
- Ticket lookup by code targets `Ticket` directly (not through `Order`).
- Future check-in/void/reissue transitions can evolve on the `Ticket` aggregate without reshaping API identity contracts.

### TicketStatus

```
Issued   (only status in M04)
```

State transitions are deferred to Milestone 05.

### Issuance Rules

- One seat order item produces exactly one ticket.
- One GA order item produces `Quantity` tickets.
- For seat-derived tickets: `InventorySeatId` is populated; `GeneralAdmissionPoolId` is null.
- For GA-derived tickets: `GeneralAdmissionPoolId` is populated; `InventorySeatId` is null.
- Ticket codes must be unique and should not expose order, buyer, or inventory internals.

## Persistence

New table:

| Table | Key columns |
|-------|------------|
| `tickets` | `id`, `order_id`, `order_item_id`, `ticket_code`, `inventory_seat_id`, `general_admission_pool_id`, `status`, `created_at` |

Required uniqueness constraints:

| Table | Constraint |
|-------|-----------|
| `tickets` | `UNIQUE(ticket_code)` |

Capability C already requires `UNIQUE(orders.reservation_id)` and remains unchanged.

## Endpoints

| Method | Path | Auth | Success |
|--------|------|------|---------|
| `POST` | `/reservations/{reservationId:guid}/checkout` | Any authenticated role | `201 Created` / `200 OK` idempotent |
| `GET` | `/orders/{orderId:guid}` | Any authenticated role | `200 OK` |
| `GET` | `/tickets/{ticketCode}` | Any authenticated role | `200 OK` |

`CheckoutReservation` and `GetOrder` are extended by this capability to include ticket data in responses.

## Request and Response Contracts

### `POST /reservations/{reservationId}/checkout`

Request body remains unchanged from Capability C.

**Response shape (new order `201` or idempotent `200`):**

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
  ],
  "tickets": [
    {
      "ticketId": "guid",
      "orderItemId": "guid",
      "ticketCode": "string",
      "status": "Issued",
      "inventorySeatId": "guid",
      "generalAdmissionPoolId": null
    }
  ]
}
```

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
  ],
  "tickets": [
    {
      "ticketId": "guid",
      "orderItemId": "guid",
      "ticketCode": "string",
      "status": "Issued",
      "inventorySeatId": "guid",
      "generalAdmissionPoolId": null
    }
  ]
}
```

### `GET /tickets/{ticketCode}`

**Response `200 OK`:**

```json
{
  "ticketId": "guid",
  "ticketCode": "string",
  "status": "Issued",
  "orderId": "guid",
  "orderItemId": "guid",
  "inventorySeatId": "guid",
  "generalAdmissionPoolId": null
}
```

**Response `404 Not Found`:** when ticket code does not exist.

## Error Paths

| Scenario | HTTP Status |
|----------|-------------|
| Ticket code not found | 404 |
| Checkout on cancelled reservation | 409 |
| Checkout on expired reservation | 409 |
| Checkout on reserved but already past `ExpiresAt` | 409 |
| Concurrency conflict (checkout race winner/loser) | 409 |

## Issuance and Idempotency Flow

1. Execute checkout validation and order creation flow from Capability C.
2. For each created `OrderItem`, generate tickets:
   - `Seat` item: create one ticket.
   - `GeneralAdmissionPool` item: create `Quantity` tickets.
3. Persist order + order items + tickets in the same transaction.
4. If checkout is replayed for an already completed reservation, return existing order and existing tickets.

Database uniqueness on `orders.reservation_id` and `tickets.ticket_code` protects idempotency and code collisions.

## Implementation Notes

### Ticket Code Generation

Ticket codes are generated by an `ITicketCodeGenerator` using high-entropy random data. Codes **must not** be derived from order, ticket, buyer, or inventory identifiers.

Responsibilities are split intentionally:

- Domain: `TicketCode` value object validates the allowed shape and canonical form.
- Application/Infrastructure: generate random codes, normalize input for lookup, and handle rare unique-constraint collisions with bounded retry.
- Database: enforce uniqueness with a unique constraint on `ticket_code`.

**Normalization:**

- Uppercase all letters.
- Trim leading/trailing whitespace.
- Remove hyphens.
- Store and compare in normalized form.

**Code Format:**

- Crockford Base32.
- 16 characters.
- Opaque and non-sequential.
- Human-friendly for manual entry scenarios.

**Generator Contract:**
`ITicketCodeGenerator` is responsible only for generating candidate codes:

```csharp
public interface ITicketCodeGenerator
{
  TicketCode Generate();
}
```

The generator does not check storage for uniqueness and does not depend on repositories or persistence concerns.

**Collision Handling:**
Collisions are treated as extremely rare in M04 due to high entropy. The application layer attempts persistence, relies on the database unique constraint as the final guard, and retries generation a small bounded number of times if a unique-constraint violation occurs.

**Database Collation:**
Define the `ticket_code` column with a **case-insensitive collation** (e.g., `COLLATE SQL_Latin1_General_CP1_CI_AS` in SQL Server). This provides defense-in-depth even if app-level normalization is bypassed, ensuring lookups and uniqueness checks remain case-insensitive.

**Uniqueness Constraint:**
`UNIQUE(ticket_code)` at the database level, combined with case-insensitive collation, prevents accidental duplicates and enforces the code's role as a lookup key.

## Files

### New Source Files

| File | Purpose |
|------|---------|
| `Domain/Ticket.cs` | `Ticket` aggregate root and `TicketStatus` enum |
| `Domain/TicketCode.cs` | Value object for canonical ticket code shape and normalization |
| `Domain/ITicketCodeGenerator.cs` | Interface for generating candidate ticket codes |
| `Infrastructure/TicketCodeGenerator.cs` | Implementation using high-entropy random data and Crockford Base32 encoding |
| `Infrastructure/Persistence/TicketConfiguration.cs` | EF Core mapping and unique case-insensitive index on `ticket_code` |
| `Features/GetTicketByCode/GetTicketByCodeErrors.cs` | Typed error factory |
| `Features/GetTicketByCode/GetTicketByCode.cs` | Query handler and result records |
| `Features/GetTicketByCode/GetTicketByCodeEndpoint.cs` | `GET /tickets/{ticketCode}` endpoint |

### Modified Source Files

| File | Change |
|------|--------|
| `Features/CheckoutReservation/CheckoutReservation.cs` | Create and return issued tickets |
| `Features/CheckoutReservation/CheckoutReservationEndpoint.cs` | Return `tickets` in response DTO |
| `Features/GetOrder/GetOrder.cs` | Include tickets in read model |
| `Features/GetOrder/GetOrderEndpoint.cs` | Return `tickets` in response DTO |
| `Infrastructure/Persistence/...DbContext` | Add `DbSet<Ticket>` and model wiring |
| `ModuleConfiguration.cs` | Register ticket configuration and `GetTicketByCode` handler |
| `ModuleEndpointMappings.cs` | Wire `MapGetTicketByCode` |

### New Test Files

| File | Covers |
|------|--------|
| `Ticketing/Tickets/GetTicketByCodeTests.cs` | D5 and 404 behavior |

### Modified Test Files

| File | Covers |
|------|--------|
| `Ticketing/Orders/CheckoutReservationTests.cs` | D1–D3 and idempotent replay returns same tickets |
| `Ticketing/Orders/GetOrderTests.cs` | D4 order retrieval includes tickets |
| `Ticketing/Authorization/TicketingAuthorizationTests.cs` | E7 for `GET /tickets/{ticketCode}` |

## Acceptance Criteria

### Ticket Issuance (D1–D3)

- [ ] Successful checkout issues tickets.
- [ ] One seat item issues exactly one ticket.
- [ ] One GA unit issues exactly one ticket.
- [ ] Issued tickets are persisted and associated to order and order item.
- [ ] New tickets start with `Status = Issued`.
- [ ] Ticket codes are unique at database level.
- [ ] Ticket codes are opaque, unique, and non-sequential.

### API Contracts (D4–D5)

- [ ] `CheckoutReservation` response includes `tickets`.
- [ ] `GetOrder` response includes `tickets`.
- [ ] `GET /tickets/{ticketCode}` returns ticket details including order and target reference.
- [ ] Unknown ticket code returns `404`.

### Idempotency and Concurrency

- [ ] Repeated checkout for an already completed reservation returns the same order and tickets.
- [ ] Duplicate checkout cannot create duplicate orders or duplicate tickets.
- [ ] In checkout vs cancel/expire races, first committed transition wins and loser returns conflict.

### Authorization (E7)

- [ ] Unauthenticated `GET /orders/{id}` returns `401`.
- [ ] Unauthenticated `GET /tickets/{ticketCode}` returns `401`.
- [ ] Authenticated non-EventManager users can access customer-facing ticket endpoints.

## Definition of Done

- [ ] All acceptance criteria met.
- [ ] Milestone 04 Capability D checkboxes D1–D5 updated.
- [ ] `dotnet build` passes at solution level.
- [ ] `dotnet test` passes at solution level.
- [ ] Milestone and issue docs reflect shipped behavior.
