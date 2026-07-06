# Issue 152 - Projection Convergence And Boundary Tests

## Status

- **Issue**: 152
- **Title**: Projection Convergence And Boundary Tests
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Ticket lifecycle projection in Attendance (Capability C, Issue 127)
- **Status**: Draft

## Problem Statement

The TicketIssued and TicketCanceled consumers need shared hardening around delivery ordering, replay behavior, and module boundaries. This issue covers the tests that prove the two handlers converge to the same final projection state under duplicate, out-of-order, and concurrent delivery while remaining a read model only.

## Scope

- In scope:
  - Add out-of-order delivery tests across both handlers
  - Add replay tests across both handlers
  - Add concurrent duplicate delivery tests
  - Add boundary tests that keep Attendance projection logic read-model only
  - Add architecture tests that keep Attendance isolated from Ticketing internals
- Out of scope:
  - New event contract changes
  - New consumer behavior beyond test coverage
  - Scan/check-in command handling

## Functional Requirements

- [ ] Projection handlers converge under out-of-order event delivery.
- [ ] Projection handlers remain idempotent across both ticket lifecycle events.
- [ ] Concurrent duplicate deliveries do not create duplicate projections.
- [ ] Projection is not authoritative for scan validity.

## Acceptance Criteria

- [ ] `TicketCanceled` before `TicketIssued` converges to a canceled or invalid projection state.
- [ ] Replayed `TicketIssued` does not revert a canceled projection to issued.
- [ ] Replayed `TicketCanceled` remains idempotent.
- [ ] Concurrent duplicate event handling does not create duplicate rows.
- [ ] Architecture tests still ensure Attendance does not depend on Ticketing internals.

## Test Checklist

- [x] Integration test: `TicketCanceled` before `TicketIssued` converges correctly.
- [x] Integration test: replayed `TicketIssued` does not reopen canceled state.
- [x] Integration test: replayed `TicketCanceled` remains idempotent.
- [x] Integration test: concurrent duplicate event handling does not create duplicate projections.
- [x] Architecture test: Attendance consumes Ticketing events/contracts only and does not depend on Ticketing internals.

## Dependencies

- Capability A event contracts and outbox delivery
- Capability B persistence model, uniqueness constraints, and indexes
- Consumers from Issues 150 and 151

## Notes

- Keep this issue focused on cross-handler behavior rather than consumer implementation details.
- If a boundary rule is needed to keep the projection read-only, test that here rather than in the consumer issues.
