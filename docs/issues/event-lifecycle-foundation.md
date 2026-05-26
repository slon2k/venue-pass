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

- [ ] An event can be created through the Events API using venue and manifest template identifiers.
- [ ] Creating an event creates a manifest snapshot that is independent from later template edits.
- [ ] An event can be retrieved through the Events API with expected core fields.
- [ ] Manifest snapshot structure is persisted correctly and can be read back for the event.
- [ ] Invalid create requests are rejected with consistent validation errors.

## Vertical Slices

- [ ] A1: Implement Event aggregate lifecycle core for Draft and Published states
- [ ] A2: Model and persist Manifest snapshot copied from selected ManifestTemplate
- [ ] A3: Deliver CreateEvent and GetEvent end-to-end API behavior
- [ ] A4: Add integration tests for create/get event and manifest snapshot structure

## Risks and Assumptions

- Snapshot shape decisions here will constrain Ticketing synchronization in Milestone 03.
- Aggregate boundary decisions may require minor adjustments once publication flow is introduced.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
