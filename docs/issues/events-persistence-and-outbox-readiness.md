# Events persistence and outbox readiness

## Summary

Provide the persistence foundation required by the Events module so venue and manifest template capabilities can be stored in the `events` schema and later extended toward publication.

## Scope

- In scope:
  - Persist venue and manifest template aggregate structure in the `events` schema
  - Add migrations required for venues and manifest templates
  - Add outbox table in the `events` schema without publication flow yet
- Out of scope:
  - Event creation and publication
  - Manifest snapshot creation from templates
  - Cross-module integration event dispatch

## Acceptance Criteria

- [x] The Events schema contains the required tables and mappings for venue and manifest template persistence.
- [x] The required migrations exist and can be applied cleanly.
- [x] The outbox table exists but publication flow is not enabled yet.

## Vertical Slices

- [x] Persist venue and manifest template aggregate structure
- [x] Add migrations for venue and manifest template storage
- [x] Add outbox table without publication flow

## Risks and Assumptions

- Persistence decisions made here should not leak publication behavior into M01.
- Outbox readiness should remain a structural enabler, not an execution feature.

## Definition of Done

- [x] Acceptance criteria met
- [x] Tests passing
- [x] Docs updated if behavior changed
