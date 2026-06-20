# Capability B: Attendance module foundation and persistence

## Summary

Establish the Attendance module baseline with persistence, schema, and core domain/read models required to support check-in, ticket projection, and scan attempt tracking.

## Scope

- In scope:
  - Add Attendance module configuration and startup wiring
  - Add `AttendanceDbContext`
  - Add `attendance` schema and migrations
  - Configure Attendance outbox usage if not inherited from shared infrastructure
  - Implement Attendance domain model:
    - `AttendanceRecord`
    - `ScanAttempt`
  - Implement Attendance ticket projection/read model
  - Add EF persistence mappings
  - Add uniqueness constraints for one successful check-in per ticket
  - Add lookup indexes for ticket ID and normalized ticket code
- Out of scope:
  - Full scan/check-in command handling
  - Ticket lifecycle projection consumers
  - Ticketing validation contract usage
  - `TicketCheckedIn` event emission
  - Operator-facing reporting UI

## Functional Requirements

- [ ] SC-05 Duplicate prevention is owned by Attendance.
- [ ] SC-06 Duplicate prevention is enforced at database level.
- [ ] SC-10 Scan attempts are persisted for accepted and rejected scans.
- [ ] SC-11 Rejected scan attempts include reason.
- [ ] AR-01 Attendance record stores mandatory identifiers and checked-in timestamp.
- [ ] AR-02 Attendance record may store optional order and inventory references.
- [ ] AR-03 Ticket can have at most one successful attendance record.
- [ ] AP-05 Projection storage supports idempotency by ticket ID.
- [ ] AP-06 Projection storage supports idempotency by ticket code.

## Persistence Requirements

- [ ] Add `attendance` schema migration.
- [ ] Create table for attendance records.
- [ ] Create table for scan attempts.
- [ ] Create table for ticket projections.
- [ ] Attendance records store:
  - attendance record ID
  - ticket ID
  - normalized ticket code
  - canonical event reference
  - checked-in timestamp
  - optional order ID
  - optional order item ID
  - optional inventory seat ID
  - optional GA pool ID
- [ ] Scan attempts store:
  - scan attempt ID
  - submitted/normalized ticket code
  - outcome
  - reason category
  - scan timestamp
  - optional ticket ID
  - optional canonical event reference
- [ ] Ticket projections store:
  - ticket ID
  - normalized ticket code
  - ticket status/projection status
  - canonical event reference
  - optional order ID
  - optional order item ID
  - optional inventory target reference
  - last updated timestamp
- [ ] Add uniqueness constraint to enforce one successful check-in per ticket.
- [ ] Add uniqueness constraint for one ticket projection per ticket ID.
- [ ] Add uniqueness constraint for one ticket projection per normalized ticket code.
- [ ] Add required indexes for ticket ID and normalized ticket code lookups.
- [ ] Cross-module identifiers are stored as scalar references; no database foreign keys are created across module boundaries.
- [ ] Attendance outbox persistence is available for later `TicketCheckedIn` emission.

## Acceptance Criteria

- [ ] Attendance module boots and applies migrations in integration environment.
- [ ] `AttendanceDbContext` includes all required sets and mappings.
- [ ] Attendance schema is isolated under `attendance`.
- [ ] Uniqueness constraints prevent duplicate successful check-ins.
- [ ] Ticket projection persistence supports idempotent upsert/update by ticket ID and ticket code.
- [ ] Scan attempt persistence supports accepted and rejected outcomes with reason category.
- [ ] Data model supports both seat and GA references.
- [ ] Attendance outbox persistence is configured or explicitly provided by shared infrastructure.
- [ ] Architecture tests pass without new module-boundary violations.

## Test Checklist

- [ ] Migration smoke test: `attendance` schema applies cleanly.
- [ ] Persistence test: one successful attendance record per ticket is enforced by database.
- [ ] Persistence test: duplicate ticket projection by ticket ID is rejected or handled deterministically.
- [ ] Persistence test: duplicate ticket projection by normalized ticket code is rejected or handled deterministically.
- [ ] Persistence test: accepted and rejected scan attempts both persist.
- [ ] Persistence test: rejected scan attempt stores reason category.
- [ ] Repository/query test: lookup by ticket ID works.
- [ ] Repository/query test: lookup by normalized ticket code works.
- [ ] Architecture test: Attendance does not depend on Ticketing internals.

## Dependencies

- Shared database and migration pipeline conventions.
- Shared BuildingBlocks persistence and outbox infrastructure.
- Canonical event reference for Attendance is agreed in Capability A.

## Risks

- Wrong uniqueness/index strategy can create race conditions or poor query performance.
- Schema naming drift can cause integration-test fragility.
- Missing canonical event reference in persistence can force schema changes during scan implementation.
- Treating ticket projections as authoritative could violate the Ticketing/Attendance boundary.
