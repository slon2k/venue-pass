# Issue 133 - Implement Ticket Cancellation

## Status

- **Issue**: 133
- **Title**: Implement Ticket Cancellation
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Ticketing Readiness for Attendance (Capability A, Issue 125)
- **Status**: Open

## Problem Statement

Attendance check-in requires canceled tickets to validate as invalid. To support this, Ticketing must expose a `CancelTicket` endpoint that transitions an issued ticket to the canceled state. The cancellation must be idempotent, atomic with its outbox write, and must emit a `TicketCanceled` integration event exactly once per cancellation.

The `Ticket` domain model already has a `Cancel()` method, and `TicketStatus.Canceled` is expected. This issue covers the command handler, endpoint, outbox integration, and the error contract for cancellation.

## Scope

- In scope:
  - Add `TicketStatus.Canceled` value if not present
  - Add `CancelTicket` command and handler
  - Add `CancelTicket` endpoint (operational role required)
  - Emit `TicketCanceled` integration event via outbox on successful cancellation
  - Ensure cancellation and outbox write are atomic
  - Return current canceled state for repeated cancellation requests without emitting a duplicate event
  - Add `TicketCanceled` error entries to `TicketErrors` if needed
- Out of scope:
  - Refunds and payment reversal
  - Order cancellation workflows
  - Inventory re-allocation on ticket cancellation
  - Attendance check-in state changes

## Functional Requirements

- [ ] EV-03 Ticketing supports minimal issued-ticket cancellation.
- [ ] EV-04 Cancellation transition is `Issued -> Canceled`.
- [ ] EV-05 Canceling an issued ticket emits `TicketCanceled`.
- [ ] EV-07 Repeated cancellation is deterministic and idempotent.
- [ ] EV-08 Cancellation does not imply refund, order reversal, inventory release, or payment reversal.
- [ ] EV-10 Repeated/idempotent cancellation does not emit duplicate `TicketCanceled` events for tickets already canceled.
- [ ] EV-12 `TicketCanceled` outbox message is written atomically with ticket cancellation.
- [ ] TR-08 Canceled ticket returns invalid result when validated (covered by issue 132; cancellation state must be persisted correctly).

## Endpoint Contract

### `DELETE /ticketing/tickets/{ticketId}`

- Auth: operational role
- Path parameter: `ticketId`

Success behavior:

- Issued ticket transitions to `Canceled`.
- Response includes current canceled ticket state.
- `TicketCanceled` outbox message is written in the same transaction.

Idempotent behavior:

- Already-canceled ticket returns current canceled state.
- No additional `TicketCanceled` outbox message is written.

Error behavior:

- Unknown ticket ID returns not-found.
- Ticket in a non-cancelable state (if any future states are added) returns an appropriate domain error.

Response minimum fields:

- ticket ID
- ticket code
- status (`Canceled`)
- canceled timestamp

## Integration Event

### `TicketCanceled`

Payload:

- ticket ID
- ticket code
- canonical event reference (`PublishedEventReferenceId`)
- canceled timestamp

Rules:

- Emitted via outbox pattern only.
- Written atomically with the cancellation state change.
- Not emitted when the ticket was already canceled.
- Safe for at-least-once delivery consumers.

## Acceptance Criteria

- [ ] `CancelTicket` endpoint exists and requires operational role.
- [ ] Issuing a cancel on an issued ticket transitions it to `Canceled`.
- [ ] `TicketCanceled` is written to the outbox in the same transaction as the status change.
- [ ] Repeated cancellation returns canceled state without writing a duplicate outbox message.
- [ ] Unknown ticket ID returns a not-found response.
- [ ] Response includes ticket ID, ticket code, status, and canceled timestamp.

## Test Checklist

- [ ] Integration test: `CancelTicket` transitions issued ticket to `Canceled`.
- [ ] Integration test: `TicketCanceled` outbox message is written on successful cancellation.
- [ ] Integration test: cancellation status update and `TicketCanceled` outbox write occur in same transaction.
- [ ] Integration test: repeated cancellation does not write a duplicate `TicketCanceled` outbox message.
- [ ] Integration test: canceled ticket returns current state on repeated cancellation.
- [ ] Integration test: unknown ticket ID returns not-found.
- [ ] Authorization test: `CancelTicket` requires operational role.

## Related Issues

- Issue 125: Ticketing readiness for attendance (parent capability)
- Issue 131: Ticketing integration event publishing (`TicketCanceled` event definition)
- Issue 132: Ticket validation contract (canceled ticket must return invalid)
- Issue 130: Integration, authorization, and concurrency tests

## Notes

- `Ticket.Cancel()` already exists and returns `false` when the ticket is already canceled — use this return value to gate outbox message creation.
- Cancellation has no payment or inventory side effects in M05.
- The canceled timestamp should be captured at the point of state transition, not from the request.
