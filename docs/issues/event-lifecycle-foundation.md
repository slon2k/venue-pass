# Event lifecycle foundation

## Summary

Establish the Events module lifecycle foundation by introducing event creation and retrieval with a manifest snapshot copied from the selected manifest template.

## Scope

- In scope:
  - Event aggregate lifecycle core (Draft and Published path foundation)
  - Manifest snapshot domain modeling and persistence for newly created events
  - Create event API flow
  - Get event API flow
  - Integration tests for create/get and snapshot structure
- Out of scope:
  - Event manager assignment
  - Event publication and outbox integration events
  - Event cancellation workflows

## Acceptance Criteria

- [ ] An event can be created through the Events API using required fields: `Name`, `EventDateUtc`, `VenueId`, and `ManifestTemplateId`.
- [ ] Event creation rejects requests where `EventDateUtc` is not in the future.
- [ ] Creating an event creates a manifest snapshot that is independent from later template edits.
- [ ] An event can be retrieved through the Events API with expected core fields.
- [ ] Manifest snapshot structure is persisted correctly and can be read back for the event.
- [ ] Invalid create requests are rejected with consistent validation errors.

## Vertical Slices

- [ ] A1: Implement Event aggregate lifecycle core for Draft and Published states
- [ ] A2: Model and persist Manifest snapshot copied from selected ManifestTemplate
- [ ] A3: Deliver CreateEvent and GetEvent end-to-end API behavior
- [ ] A4: Add integration tests for create/get event and manifest snapshot structure

## Functional Decisions Required Before A3

- [x] Required event fields at creation are fixed for M02 (`Name`, `EventDateUtc`, `VenueId`, `ManifestTemplateId`).
- [x] `ManifestTemplateId` is required at creation for M02.
- [x] Attach/replace manifest flow is deferred from M02.
- [x] Publication preconditions in `Draft` are fixed in milestone decision log.
- [x] API contract date-time representation is UTC.

Reference: align with the Functional Requirements Baseline and Decision Log in milestone file.

## Risks and Assumptions

- Snapshot shape decisions here will constrain Ticketing synchronization in Milestone 03.
- Aggregate boundary decisions may require minor adjustments once publication flow is introduced.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
