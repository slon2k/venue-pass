# Issue 137 - Attendance Persistence Foundation

## Status

- **Issue**: 137
- **Title**: Attendance Persistence Foundation
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Attendance module foundation and persistence (Capability B, Issue 126)
- **Status**: Open

## Problem Statement

Attendance needs a dedicated persistence baseline before domain workflows can be implemented. The module must own an isolated `attendance` schema, initial migration flow, EF conventions, and outbox infrastructure so later domain and projection persistence can be added safely after modeling.

This issue establishes the database foundation so later Attendance features can rely on stable schema and EF conventions.

## Scope

- In scope:
  - Add `attendance` schema migration baseline
  - Establish Attendance migration pipeline and schema ownership conventions
  - Configure Attendance outbox persistence if shared infrastructure does not already provide it
  - Add Attendance outbox dispatcher integration tests
  - Ensure cross-module identifiers are stored as scalar references only
- Out of scope:
  - AttendanceRecord, ScanAttempt, and TicketProjection table design
  - Attendance domain and projection persistence mappings
  - Uniqueness constraints and lookup indexes for AttendanceRecord and TicketProjection
  - Scan/check-in command flow
  - Ticket lifecycle event consumers
  - Ticketing validation contract calls
  - Retrieval endpoints
  - Projection update business logic

## Functional Requirements

- [ ] Add `attendance` schema migration.
- [ ] Attendance migration pipeline is established for future schema evolution.
- [ ] Cross-module identifiers are stored as scalar references with no cross-module foreign keys.
- [ ] Attendance outbox persistence is available for later `TicketCheckedIn` emission, if not inherited from shared infrastructure.
- [ ] Attendance outbox dispatcher behavior is covered by integration tests.

## Persistence Design Notes

- Keep schema objects isolated under `attendance`.
- Record/projection uniqueness constraints are intentionally deferred until model shape is finalized in Issues 138 and 139.
- Outbox setup should be conditional on actual shared-infrastructure coverage; do not duplicate infrastructure unnecessarily.

## Acceptance Criteria

- [ ] Attendance migration applies cleanly from a clean database.
- [ ] Attendance outbox table exists under the `attendance` schema.
- [ ] Attendance outbox dispatcher integration tests pass.
- [ ] No cross-module foreign keys are introduced.
- [ ] Attendance outbox persistence is either configured or explicitly confirmed as inherited.

## Test Checklist

- [ ] Migration smoke test: `attendance` schema applies cleanly.
- [ ] Schema verification test: outbox table is created under `attendance`.
- [ ] Outbox dispatcher test: eligible message is dispatched and marked processed.
- [ ] Outbox dispatcher test: failed dispatch increments attempts and schedules retry.
- [ ] Outbox dispatcher test: max-attempt message is abandoned deterministically.

## Related Issues

- Issue 126: Attendance module foundation and persistence (parent capability)
- Issue 136: Attendance module wiring and DbContext
- Issue 138: Attendance domain model for attendance records and scan attempts
- Issue 139: Attendance ticket projection read model
- Issue 140: Attendance foundation persistence and architecture tests

## Notes

- This issue is about schema and outbox infrastructure baseline only.
- Attendance record and ticket projection persistence surfaces are intentionally delivered in Issues 138 and 139 after model finalization.
- If outbox infrastructure is already inherited from shared building blocks, document that explicitly instead of re-implementing it.
