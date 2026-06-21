# Capability A: Ticketing readiness for Attendance

## Summary

Deliver Ticketing-side contracts and lifecycle behaviors required for Attendance check-in. This capability adds ticket validation access, minimal cancellation, and lifecycle integration events so Attendance can validate tickets and project ticket state changes.

## Scope

- In scope:
  - Add public Ticketing module contract for validation:
    - `ITicketingModule.GetTicketForValidationAsync(...)`
  - Implement Ticketing validation query for ticket code input
  - Ensure validation response includes identifiers and status required by Attendance
  - Agree and use one canonical event reference for Attendance
  - Emit `TicketIssued` during successful checkout
  - Add `TicketStatus.Canceled`
  - Add minimal `CancelTicket` behavior and endpoint
  - Emit `TicketCanceled` when cancellation succeeds
  - Ensure canceled tickets validate as invalid
- Out of scope:
  - Refunds and payment reversal
  - Order cancellation workflows
  - Inventory re-allocation on ticket cancellation
  - Attendance check-in state changes
  - Offline scan validation

## Functional Requirements

### Ticket Validation Requirements

- [ ] TR-01 Ticketing exposes public ticket validation contract.
- [ ] TR-02 Attendance can consume Ticketing validation through public contract only.
- [ ] TR-03 Validation contract returns DTO/contract types only, not Ticketing domain entities.
- [ ] TR-04 Validation accepts ticket code input.
- [ ] TR-05 Validation normalizes ticket code using Ticketing rules.
- [ ] TR-06 Unknown ticket returns invalid or not-found result.
- [ ] TR-07 Issued ticket returns valid result.
- [ ] TR-08 Canceled ticket returns invalid result.
- [ ] TR-09 Validation response includes current ticket status.
- [ ] TR-10 Validation response includes stable identifiers:
  - ticket ID
  - ticket code
  - order ID
  - order item ID
  - canonical event reference
  - inventory target reference
- [ ] TR-11 Validation handles malformed ticket code input deterministically.
- [ ] TR-12 Malformed ticket code does not throw an unhandled exception.
- [ ] TR-13 Canonical event reference exposed to Attendance is agreed and used consistently.

### Ticket Lifecycle Event Requirements

- [ ] EV-01 Checkout emits one `TicketIssued` event per issued ticket.
- [ ] EV-02 `TicketIssued` payload includes required identifiers and timestamp:
  - ticket ID
  - ticket code
  - order ID
  - order item ID
  - canonical event reference
  - inventory target reference
  - issued timestamp
- [ ] EV-03 Ticketing supports minimal issued-ticket cancellation.
- [ ] EV-04 Cancellation transition is `Issued -> Canceled`.
- [ ] EV-05 Canceling ticket emits `TicketCanceled`.
- [ ] EV-06 `TicketCanceled` payload includes required identifiers and timestamp:
  - ticket ID
  - ticket code
  - canonical event reference
  - canceled timestamp
- [ ] EV-07 Repeated cancellation is deterministic and idempotent.
- [ ] EV-08 Cancellation does not imply refund, order reversal, inventory release, or payment reversal in M05.
- [ ] EV-09 Repeated/idempotent checkout does not emit duplicate `TicketIssued` events for tickets already issued.
- [ ] EV-10 Repeated/idempotent cancellation does not emit duplicate `TicketCanceled` events for tickets already canceled.
- [ ] EV-11 `TicketIssued` outbox messages are written atomically with checkout/ticket creation.
- [ ] EV-12 `TicketCanceled` outbox message is written atomically with ticket cancellation.

## API and Event Contracts

### Ticket validation contract

- Input:
  - ticket code
- Output minimum:
  - ticket ID
  - ticket code
  - status
  - is valid flag
  - order ID
  - order item ID
  - canonical event reference
  - inventory target reference
  - invalid/not-found/malformed reason when validation fails

Contract rules:

- Contract returns DTOs only; it does not expose Ticketing domain entities.
- Ticket code input is normalized using Ticketing ticket-code rules.
- Malformed ticket code returns an invalid validation result with explicit reason.
- Unknown ticket code returns invalid or not-found result.
- Canceled ticket returns invalid result.
- Issued ticket returns valid result.
- Validation response and Ticketing lifecycle events use the same canonical event reference.

Suggested invalid reasons:

- `MalformedTicketCode`
- `UnknownTicket`
- `CanceledTicket`

### `CancelTicket` endpoint

- Auth: operational role
- Success behavior:
  - ticket transitions to canceled when currently issued
  - repeated cancellation of an already canceled ticket returns current canceled ticket state
  - repeated cancellation does not emit another `TicketCanceled` event
  - response is deterministic for repeated cancellation attempts

### Outbound events

- `TicketIssued`
- `TicketCanceled`

Event rules:

- Both events must be emitted via outbox pattern.
- Both events must be safe for at-least-once delivery consumers.
- Event payloads use the same canonical event reference as validation response.
- `TicketIssued` is written atomically with checkout/ticket creation.
- `TicketCanceled` is written atomically with ticket cancellation.
- Idempotent checkout must not create duplicate `TicketIssued` messages for already-issued tickets.
- Idempotent cancellation must not create duplicate `TicketCanceled` messages for already-canceled tickets.

## Acceptance Criteria

- [ ] Attendance can call Ticketing validation through public module contract only.
- [ ] Validation returns DTO/contract types, not domain entities.
- [ ] Validation handles malformed ticket code without unhandled exception.
- [ ] Validation for issued tickets returns valid.
- [ ] Validation for canceled tickets always returns invalid.
- [ ] Validation for unknown tickets returns invalid or not-found.
- [ ] Validation response and lifecycle events use the same canonical event reference.
- [ ] Checkout emits `TicketIssued` for each newly issued ticket.
- [ ] Repeated checkout does not emit duplicate `TicketIssued` events.
- [ ] `CancelTicket` transitions issued ticket to canceled.
- [ ] `CancelTicket` emits `TicketCanceled` when cancellation succeeds.
- [ ] Repeated cancellation requests return canceled state and do not emit duplicate `TicketCanceled`.
- [ ] Ticket cancellation and `TicketCanceled` outbox write are atomic.

## Test Checklist

- [ ] Contract test: issued ticket validates as valid.
- [ ] Contract test: canceled ticket validates as invalid.
- [ ] Contract test: unknown ticket validates as invalid or not found.
- [ ] Contract test: malformed ticket code returns invalid/malformed result.
- [ ] Integration test: checkout writes `TicketIssued`.
- [ ] Integration test: repeated checkout does not write duplicate `TicketIssued`.
- [ ] Integration test: cancellation writes `TicketCanceled`.
- [ ] Integration test: repeated cancellation does not write duplicate `TicketCanceled`.
- [ ] Integration test: ticket cancellation status update and `TicketCanceled` outbox write occur in same transaction.
- [ ] Authorization test: `CancelTicket` requires operational role.

## Dependencies

- M04 checkout and ticket issuance must remain stable.
- Existing outbox infrastructure for Ticketing must be operational.
- Canonical event reference for Attendance must be agreed before implementation.

## Risks

- Contract drift between Ticketing and Attendance can break scan flow.
- Ambiguous cancellation semantics can cause inconsistent operator behavior.
- Duplicate lifecycle events may corrupt Attendance projection if idempotency assumptions are violated.
- Missing or inconsistent event reference can make Attendance records hard to associate with events.
