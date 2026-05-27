# Event publication and integration

## Summary

Enable event publication by enforcing lifecycle guards, locking structural manifest edits, and emitting EventPublished through the outbox dispatcher path.

## Scope

- In scope:
  - Publish event lifecycle transition and guards
  - Structural manifest immutability after publication
  - Outbox write for EventPublished(EventId, ManifestId)
  - Outbox dispatcher processing path for EventPublished
  - Integration tests for outbox write and dispatch observability
- Out of scope:
  - Ticketing subscriber/event sync behavior (Milestone 03)
  - Event cancellation flows
  - Cross-module retries/dead-letter hardening beyond milestone-level needs

## Acceptance Criteria

- [ ] A draft event can be published through the Events API.
- [ ] Publishing is rejected when required manifest state is missing.
- [ ] Publishing is rejected when current UTC time is greater than or equal to `EventDateUtc`.
- [ ] Publishing writes EventPublished(EventId, ManifestId) to outbox.
- [ ] Outbox dispatcher processes EventPublished and processing is observable in tests/logs.
- [ ] Structural changes to manifest are rejected after publication.

## Vertical Slices

- [ ] C1: Implement PublishEvent state transition guards, including no publish without manifest
- [ ] C2: Enforce manifest structural immutability after publication
- [ ] C3: Write EventPublished(EventId, ManifestId) to outbox on publication
- [ ] C4: Ensure outbox dispatcher processes EventPublished reliably
- [ ] C5: Add integration tests for outbox write and dispatch observability

## Risks and Assumptions

- Dispatcher reliability and idempotency may require follow-up hardening work.
- Event contract shape must stay stable to reduce rework in Milestone 03 integration slices.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
