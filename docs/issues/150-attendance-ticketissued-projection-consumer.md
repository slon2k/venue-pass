# Issue 150 - Attendance TicketIssued Projection Consumer

## Status

- **Issue**: 150
- **Title**: Attendance TicketIssued Projection Consumer
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Ticket lifecycle projection in Attendance (Capability C, Issue 127)
- **Status**: Draft

## Problem Statement

Attendance needs a consumer for `TicketIssued` so it can maintain the local ticket projection used by check-in and retrieval flows. This issue covers the event contract shape needed to carry target identifiers, the Ticketing emission update, and the Attendance-side handler that stores or updates the projection on first delivery and on replay.

## Scope

- In scope:
  - Update `TicketIssued` contract target identifiers as needed for the projection
  - Update Ticketing emission for `TicketIssued`
  - Implement Attendance `TicketIssued` projection handler
  - Upsert projection by ticket ID
  - Upsert projection by normalized ticket code
  - Keep projection read-model only
  - Add idempotency tests for repeated `TicketIssued` deliveries
- Out of scope:
  - `TicketCanceled` handling
  - Scan/check-in command handling
  - Projection convergence tests that combine both handlers
  - Projection-based authorization logic

## Functional Requirements

- [ ] Attendance receives `TicketIssued`.
- [ ] `TicketIssued` stores or updates the local projection.
- [ ] Projection handlers are idempotent by ticket ID.
- [ ] Projection handlers are idempotent by ticket code.
- [ ] Projection remains a read model only.

## Acceptance Criteria

- [ ] `TicketIssued` creates a projection when missing.
- [ ] `TicketIssued` updates the existing projection when already present.
- [ ] Duplicate `TicketIssued` deliveries do not create duplicate rows.
- [ ] Duplicate `TicketIssued` deliveries do not change the projection into an invalid state.
- [ ] The consumer does not expose the projection as an authoritative validity source.

## Test Checklist

- [ ] Integration test: `TicketIssued` creates projection.
- [ ] Integration test: replayed `TicketIssued` remains idempotent.
- [ ] Integration test: duplicate `TicketIssued` deliveries do not create duplicate projections.
- [ ] Architecture test: Attendance still depends only on Ticketing contracts, not internals.

## Dependencies

- Capability A event contracts and outbox delivery
- Capability B persistence model, uniqueness constraints, and indexes
- `TicketIssued` contract shape decision for target identifiers

## Notes

- Keep the handler tolerant of at-least-once delivery.
- Treat projection writes as local read-model updates only.
