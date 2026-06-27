# Issue 138 - Attendance Domain Model Records And Scan Attempts

## Status

- **Issue**: 138
- **Title**: Attendance Domain Model Records And Scan Attempts
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Attendance module foundation and persistence (Capability B, Issue 126)
- **Status**: Open

## Problem Statement

Attendance needs core domain entities to represent successful check-ins and operational scan history. `AttendanceRecord` will represent one successful admission outcome per ticket, while `ScanAttempt` will capture accepted and rejected scan attempts, including rejection reasons required for later operator workflows and diagnostics.

This issue defines the Attendance-owned domain surface for check-in persistence without yet implementing the scan command flow.

## Scope

- In scope:
  - Implement `AttendanceRecord`
  - Implement `ScanAttempt`
  - Add value objects or enums needed for scan outcome and rejection reason category
  - Persist required identifiers and timestamps
  - Add persistence mappings for `AttendanceRecord` and `ScanAttempt`
  - Add database uniqueness constraint for one successful attendance record per ticket
  - Support both seat and GA references
  - Align entity shape with Capability B persistence requirements
- Out of scope:
  - Scan endpoint or command handling
  - Ticket projection consumer behavior
  - Ticketing validation integration
  - `TicketCheckedIn` event emission
  - Retrieval API behavior

## Functional Requirements

- [ ] AR-01 Attendance record stores mandatory identifiers and checked-in timestamp.
- [ ] AR-02 Attendance record may store optional order and inventory references.
- [ ] AR-03 Ticket can have at most one successful attendance record.
- [ ] SC-10 Scan attempts are persisted for accepted and rejected scans.
- [ ] SC-11 Rejected scan attempts include reason.
- [ ] Data model supports both seat and GA references.

## Domain Requirements

### `AttendanceRecord`

Must store:

- attendance record ID
- ticket ID
- normalized ticket code
- canonical event reference
- checked-in timestamp
- optional order ID
- optional order item ID
- optional inventory seat ID
- optional GA pool ID

### `ScanAttempt`

Must store:

- scan attempt ID
- submitted or normalized ticket code
- outcome
- reason category for rejected attempts
- scan timestamp
- optional ticket ID
- optional canonical event reference

## Acceptance Criteria

- [ ] `AttendanceRecord` and `ScanAttempt` are implemented in the Attendance domain.
- [ ] Attendance record model can represent one successful check-in per ticket.
- [ ] Scan attempt model can represent accepted and rejected outcomes.
- [ ] Rejected scan attempts carry a reason category.
- [ ] Entity model supports both seat and GA references where applicable.
- [ ] Persistence mappings align with the Attendance schema baseline.
- [ ] Database uniqueness constraint enforces one successful attendance record per ticket.

## Test Checklist

- [ ] Domain test: Attendance record requires mandatory identifiers and checked-in timestamp.
- [ ] Domain test: Attendance record supports optional order and inventory references.
- [ ] Domain test: Scan attempt supports accepted outcome.
- [ ] Domain test: Scan attempt supports rejected outcome with reason category.
- [ ] Persistence test: accepted and rejected scan attempts both persist.
- [ ] Persistence test: rejected scan attempt stores reason category.
- [ ] Persistence test: duplicate successful attendance record for same ticket is rejected deterministically.

## Related Issues

- Issue 126: Attendance module foundation and persistence (parent capability)
- Issue 137: Attendance persistence foundation
- Issue 139: Attendance ticket projection read model
- Issue 140: Attendance foundation persistence and architecture tests

## Notes

- Keep the domain model focused on Attendance-owned facts, not Ticketing state transitions.
- Avoid introducing scan command semantics here; this issue only defines the state model.
