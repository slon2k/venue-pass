# Issue 137 - Attendance Persistence Foundation

## Status

- **Issue**: 137
- **Title**: Attendance Persistence Foundation
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Attendance module foundation and persistence (Capability B, Issue 126)
- **Status**: Open

## Problem Statement

Attendance needs a dedicated persistence baseline before domain workflows can be implemented. The module must own an isolated `attendance` schema, initial migration flow, EF mappings, and the persistence surfaces required for future `AttendanceRecord`, `ScanAttempt`, `TicketProjection`, and outbox usage.

This issue establishes the database foundation so later Attendance features can rely on stable schema and EF conventions.

## Scope

- In scope:
  - Add `attendance` schema migration
  - Add Attendance DbSets for required persistence models
  - Add initial EF persistence mappings for Attendance aggregates/read models
  - Configure uniqueness constraints and indexes required by Capability B
  - Ensure cross-module identifiers are stored as scalar references only
  - Configure Attendance outbox persistence if shared infrastructure does not already provide it
- Out of scope:
  - Scan/check-in command flow
  - Ticket lifecycle event consumers
  - Ticketing validation contract calls
  - Retrieval endpoints
  - Projection update business logic

## Functional Requirements

- [ ] Add `attendance` schema migration.
- [ ] Create persistence surfaces for attendance records, scan attempts, and ticket projections.
- [ ] Add uniqueness constraint to enforce one successful attendance record per ticket.
- [ ] Add uniqueness constraint for one ticket projection per ticket ID.
- [ ] Add uniqueness constraint for one ticket projection per normalized ticket code.
- [ ] Add lookup indexes for ticket ID and normalized ticket code.
- [ ] Cross-module identifiers are stored as scalar references with no cross-module foreign keys.
- [ ] Attendance outbox persistence is available for later `TicketCheckedIn` emission, if not inherited from shared infrastructure.

## Persistence Design Notes

- Keep schema objects isolated under `attendance`.
- Use normalized ticket code persistence consistently across record/projection lookups.
- Apply uniqueness constraints at the database level to support race-safe duplicate prevention.
- Outbox setup should be conditional on actual shared-infrastructure coverage; do not duplicate infrastructure unnecessarily.

## Acceptance Criteria

- [ ] Attendance migration applies cleanly from a clean database.
- [ ] Attendance tables exist under the `attendance` schema.
- [ ] Required uniqueness constraints and indexes are present.
- [ ] `AttendanceDbContext` exposes required sets for upcoming domain and projection work.
- [ ] No cross-module foreign keys are introduced.
- [ ] Attendance outbox persistence is either configured or explicitly confirmed as inherited.

## Test Checklist

- [ ] Migration smoke test: `attendance` schema applies cleanly.
- [ ] Schema verification test: tables are created under `attendance`.
- [ ] Persistence verification: uniqueness constraint exists for one successful attendance record per ticket.
- [ ] Persistence verification: uniqueness constraint exists for one ticket projection per ticket ID.
- [ ] Persistence verification: uniqueness constraint exists for one ticket projection per normalized ticket code.
- [ ] Persistence verification: lookup indexes for ticket ID and normalized ticket code exist.

## Related Issues

- Issue 126: Attendance module foundation and persistence (parent capability)
- Issue 136: Attendance module wiring and DbContext
- Issue 138: Attendance domain model for attendance records and scan attempts
- Issue 139: Attendance ticket projection read model
- Issue 140: Attendance foundation persistence and architecture tests

## Notes

- This issue is about persistence shape and migration baseline, not behavior.
- If outbox infrastructure is already inherited from shared building blocks, document that explicitly instead of re-implementing it.
