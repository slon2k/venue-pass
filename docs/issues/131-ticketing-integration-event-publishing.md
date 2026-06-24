# Issue 131 - Ticketing Integration Event Publishing

## Status

- **Issue**: 131
- **Title**: Ticketing Integration Event Publishing
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Ticketing Readiness for Attendance (Capability A)
- **Status**: Open

## Problem Statement

Ticketing module needs to emit integration events when key lifecycle events occur:

1. `TicketIssued` - when a ticket is successfully created during checkout
2. `TicketCanceled` - when a ticket is canceled

Attendance module will consume these events to maintain local ticket projections, enabling efficient ticket lifecycle tracking without direct Ticketing database queries.

## Acceptance Criteria

- [x] Define `TicketIssued` integration event
  - ticket ID
  - ticket code
  - order ID
  - order item ID
  - event or published event reference
  - inventory target reference (seat or GA)
  - issued timestamp
- [x] Define `TicketCanceled` integration event
  - ticket ID
  - ticket code
  - event or published event reference
  - canceled timestamp
- [ ] Emit `TicketIssued` on successful checkout (after ticket creation)
- [ ] Emit `TicketCanceled` when ticket is canceled
- [ ] Events are idempotent (repeated operations don't emit duplicate events)
- [ ] Events published through Ticketing module outbox
- [ ] Integration tests validate event emission on happy path
- [ ] Integration tests validate event content includes all required fields

## Related Issues

- Issue 125: Ticketing readiness for attendance
- Issue 126: Attendance module foundation and persistence
- Issue 130: Integration, authorization, and concurrency tests

## Notes

- Ticketing module already has outbox infrastructure (inherited from Events module pattern)
- Events should be published synchronously during command handlers but persisted via outbox for reliable delivery
- Attendance will subscribe to these events in a later issue
