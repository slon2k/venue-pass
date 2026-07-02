# Issue 151 - Attendance TicketCanceled Projection Consumer

## Status

- **Issue**: 151
- **Title**: Attendance TicketCanceled Projection Consumer
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Ticket lifecycle projection in Attendance (Capability C, Issue 127)
- **Status**: Draft

## Problem Statement

Attendance needs a consumer for `TicketCanceled` so the local ticket projection can reflect terminal cancellation state. This issue covers the event-shape decision for cancellation payloads, the Attendance cancel handler, and the convergence rule that keeps canceled projections canceled even if older issued events are replayed later.

## Scope

- In scope:
  - Decide or update the `TicketCanceled` event shape
  - Implement Attendance `TicketCanceled` projection handler
  - Mark the projection canceled or invalid on delivery
  - Support the canceled-before-issued case
  - Keep canceled state terminal for the projection
  - Add idempotency tests for repeated `TicketCanceled` deliveries
- Out of scope:
  - `TicketIssued` handling
  - Scan/check-in command handling
  - Combined convergence and replay tests across both handlers
  - Projection-based authorization logic

## Functional Requirements

- [ ] Attendance receives `TicketCanceled`.
- [ ] `TicketCanceled` marks the projection canceled or invalid.
- [ ] Projection handlers are idempotent by ticket ID.
- [ ] Projection handlers are idempotent by ticket code.
- [ ] A later replayed `TicketIssued` does not reopen a canceled projection.

## Acceptance Criteria

- [ ] `TicketCanceled` marks an existing projection invalid or canceled.
- [ ] `TicketCanceled` creates or keeps a canceled projection when it arrives before `TicketIssued`.
- [ ] Duplicate `TicketCanceled` deliveries are idempotent.
- [ ] A replayed or delayed `TicketIssued` does not revert canceled state.
- [ ] The consumer does not expose the projection as an authoritative validity source.

## Test Checklist

- [ ] Integration test: `TicketCanceled` invalidates projection.
- [ ] Integration test: replayed `TicketCanceled` remains idempotent.
- [ ] Integration test: `TicketCanceled` arriving before `TicketIssued` creates or keeps canceled projection.
- [ ] Integration test: replayed or delayed `TicketIssued` does not revert canceled projection to issued.

## Dependencies

- Capability A event contracts and outbox delivery
- Capability B persistence model, uniqueness constraints, and indexes
- `TicketCanceled` contract shape decision

## Notes

- Cancelled state must remain terminal for the read model.
- Convergence rules should favor the latest known canceled state over stale issued replays.
