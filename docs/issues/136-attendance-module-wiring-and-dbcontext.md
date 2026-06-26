# Issue 136 - Attendance Module Wiring And DbContext

## Status

- **Issue**: 136
- **Title**: Attendance Module Wiring And DbContext
- **Milestone**: 05 - Attendance: Check-In
- **Parent Capability**: Attendance module foundation and persistence (Capability B, Issue 126)
- **Status**: Open

## Problem Statement

Attendance needs a minimal module shell before persistence and domain work can land safely. The module must be registered in the application, boot with its own `AttendanceDbContext`, follow schema conventions under `attendance`, and integrate into the existing application startup pipeline without leaking Ticketing internals or bypassing module boundaries.

This issue establishes the Attendance module as a first-class application module so the remaining foundation work can build on a stable composition root.

## Scope

- In scope:
  - Add Attendance module service registration and startup wiring
  - Add `AttendanceDbContext`
  - Define Attendance schema conventions
  - Register Attendance module in API startup
  - Add required EF Core configuration discovery for Attendance
  - Add initial DbSets placeholders needed by upcoming persistence issues
- Out of scope:
  - Attendance schema migration content
  - Domain entity implementation details
  - Ticket projection behavior
  - Scan/check-in endpoints or handlers
  - Ticketing validation usage

## Functional Requirements

- [ ] Attendance module can be registered from application startup.
- [ ] `AttendanceDbContext` is resolved through DI.
- [ ] Attendance module uses isolated `attendance` schema conventions.
- [ ] EF model configuration loading is set up for Attendance assembly.
- [ ] Module startup does not introduce cross-module database foreign keys.
- [ ] Module wiring remains compatible with existing integration test bootstrap.

## Implementation Notes

- Follow the same module registration approach used by the existing bounded modules.
- Keep Attendance dependencies limited to shared building blocks and public contracts only.
- If placeholders are needed for future DbSets, keep them minimal and aligned with upcoming issues.
- Favor solution-consistent startup patterns over introducing Attendance-specific bootstrapping abstractions.

## Acceptance Criteria

- [ ] Attendance module is registered in the application startup path.
- [ ] `AttendanceDbContext` can be resolved from DI in runtime and integration test environments.
- [ ] Attendance EF Core model uses the `attendance` schema by default.
- [ ] Module boots without breaking existing modules.
- [ ] Architecture boundaries remain intact.

## Test Checklist

- [ ] Build verifies Attendance module wiring compiles cleanly.
- [ ] Integration smoke test: application starts with Attendance module enabled.
- [ ] DI test or integration check: `AttendanceDbContext` resolves successfully.
- [ ] Architecture test: Attendance does not depend on Ticketing internals.

## Related Issues

- Issue 126: Attendance module foundation and persistence (parent capability)
- Issue 137: Attendance persistence foundation
- Issue 138: Attendance domain model for attendance records and scan attempts

## Notes

- This issue should land before schema-heavy or domain-heavy Attendance work.
- Keep the Attendance startup surface minimal until the persistence model is in place.
