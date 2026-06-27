# Issue 139 - Attendance Ticket Projection Read Model

## Status

- **Issue**: 139
- **Title**: Attendance Ticket Projection Read Model
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Attendance module foundation and persistence (Capability B, Issue 126)
- **Status**: Open

## Problem Statement

Attendance needs a local ticket projection/read model so check-in and retrieval flows can resolve ticket identity and current locally known ticket lifecycle state without direct Ticketing database access. The projection must support deterministic lookup by both ticket ID and normalized ticket code, and its persistence shape must be safe for idempotent updates from Ticketing lifecycle events.

This issue establishes the projection model and persistence contract, not the event-consumer behavior that will update it.

## Scope

- In scope:
  - Implement Attendance ticket projection/read model entity
  - Persist ticket ID and normalized ticket code
  - Persist projected ticket status and canonical event reference
  - Persist optional order and inventory target references
  - Persist last updated timestamp
  - Add projection persistence mapping in Attendance schema
  - Add uniqueness constraints for ticket ID and normalized ticket code
  - Add lookup indexes for ticket ID and normalized ticket code
  - Add uniqueness and lookup behavior required for idempotent projection persistence
- Out of scope:
  - `TicketIssued` or `TicketCanceled` event consumer implementation
  - Scan endpoint logic
  - Retrieval endpoint implementation
  - Ticketing validation contract usage

## Functional Requirements

- [ ] AP-05 Projection storage supports idempotency by ticket ID.
- [ ] AP-06 Projection storage supports idempotency by ticket code.
- [ ] Ticket projection stores canonical event reference.
- [ ] Ticket projection stores normalized ticket code.
- [ ] Ticket projection stores optional order and inventory target references.
- [ ] Ticket projection stores last updated timestamp.

## Projection Persistence Requirements

Projection must store:
- ticket ID
- normalized ticket code
- ticket status or projection status
- canonical event reference
- optional order ID
- optional order item ID
- optional inventory seat ID or GA pool ID
- last updated timestamp

Database behavior:
- one projection per ticket ID
- one projection per normalized ticket code
- required indexes for ticket ID and normalized ticket code lookup
- persistence shape supports deterministic upsert/update in later event-consumer work

## Acceptance Criteria

- [ ] Attendance ticket projection entity is implemented.
- [ ] Persistence shape supports lookup by ticket ID.
- [ ] Persistence shape supports lookup by normalized ticket code.
- [ ] Uniqueness constraints prevent duplicate projections by ticket ID or normalized ticket code.
- [ ] Projection entity carries canonical event reference and required stable identifiers.
- [ ] Projection persistence is ready for later idempotent event consumption.
- [ ] Lookup indexes support deterministic read-path performance by ticket ID and normalized ticket code.

## Test Checklist

- [ ] Persistence test: duplicate ticket projection by ticket ID is rejected or handled deterministically.
- [ ] Persistence test: duplicate ticket projection by normalized ticket code is rejected or handled deterministically.
- [ ] Repository or query test: lookup by ticket ID works.
- [ ] Repository or query test: lookup by normalized ticket code works.
- [ ] Persistence test: projection stores canonical event reference and last updated timestamp.

## Related Issues

- Issue 126: Attendance module foundation and persistence (parent capability)
- Issue 127: Ticket lifecycle projection in Attendance
- Issue 137: Attendance persistence foundation
- Issue 140: Attendance foundation persistence and architecture tests

## Notes

- Treat this as a local Attendance read model only; it must not become a substitute for live Ticketing validation.
- Keep status naming explicit so future retrieval responses do not confuse projection state with Attendance-owned check-in state.
