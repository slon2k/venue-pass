# Issue 140 - Attendance Foundation Persistence And Architecture Tests

## Status

- **Issue**: 140
- **Title**: Attendance Foundation Persistence And Architecture Tests
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Attendance module foundation and persistence (Capability B, Issue 126)
- **Status**: Open

## Problem Statement

Attendance foundation work introduces new module boundaries, schema objects, uniqueness constraints, and persistence models that will underpin scan and retrieval flows. Before building higher-level behavior, the foundation needs automated verification that migrations apply cleanly, persistence rules hold at the database level, lookup paths behave correctly, and Attendance does not violate module-boundary constraints.

This issue adds the baseline persistence and architecture tests required to trust the Attendance foundation.

Note: uniqueness and lookup validation tests in this issue execute after Issues 138 and 139 land the corresponding model mappings and constraints.

## Scope

- In scope:
  - Attendance migration smoke test
  - Persistence tests for attendance record uniqueness
  - Persistence tests for ticket projection uniqueness and lookup behavior
  - Persistence tests for accepted and rejected scan attempts
  - Architecture tests for module boundaries
  - Verification that Attendance does not depend on Ticketing internals
- Out of scope:
  - End-to-end scan/check-in flow tests
  - Ticket lifecycle event consumer ordering tests
  - Retrieval API tests
  - Authorization tests for future Attendance endpoints
  - Ticketing validation timeout/resilience tests

## Required Coverage

- [ ] Migration smoke test: `attendance` schema applies cleanly.
- [ ] Persistence test: one successful attendance record per ticket is enforced by database.
- [ ] Persistence test: duplicate ticket projection by ticket ID is rejected or handled deterministically.
- [ ] Persistence test: duplicate ticket projection by normalized ticket code is rejected or handled deterministically.
- [ ] Persistence test: accepted and rejected scan attempts both persist.
- [ ] Persistence test: rejected scan attempt stores reason category.
- [ ] Repository or query test: lookup by ticket ID works.
- [ ] Repository or query test: lookup by normalized ticket code works.
- [ ] Architecture test: Attendance does not depend on Ticketing internals.
- [ ] Architecture tests pass without new module-boundary violations.

## Acceptance Criteria

- [ ] Attendance schema migration is exercised in automated tests.
- [ ] Database-level uniqueness rules are covered by tests.
- [ ] Scan attempt persistence for accepted and rejected outcomes is covered by tests.
- [ ] Projection lookup behavior is covered by tests.
- [ ] Architecture tests explicitly protect the Attendance to Ticketing boundary.
- [ ] Test suite is stable enough to support upcoming Attendance feature work.

## Related Issues

- Issue 126: Attendance module foundation and persistence (parent capability)
- Issue 130: Integration, authorization, and concurrency tests
- Issue 137: Attendance persistence foundation
- Issue 138: Attendance domain model for attendance records and scan attempts
- Issue 139: Attendance ticket projection read model

## Notes

- Keep this issue focused on the Attendance foundation baseline, not the full M05 test matrix from Issue 130.
- Database-level uniqueness tests are especially important because duplicate prevention is an explicit Attendance responsibility.
