# Milestone 02 - Events: Event Creation and Publication

## Milestone Outcome

Event lifecycle and publication flow are operational end-to-end, including outbox event emission and dispatch.

## Delivery Model

Milestone 02 is delivered through parent feature (capability) issues and vertical-slice sub-issues. Each slice includes domain behavior, persistence impact, endpoint behavior, and integration tests in the same PR.

## In Scope

- [ ] Capability A: Event lifecycle foundation
- [ ] Capability B: Event staffing
- [ ] Capability C: Publication and integration

## Capability Breakdown

### Capability A: Event lifecycle foundation

- [ ] A1: Implement Event aggregate lifecycle core for Draft and Published states
- [ ] A2: Model and persist Manifest snapshot copied from selected ManifestTemplate
- [ ] A3: Deliver CreateEvent and GetEvent end-to-end API behavior
- [ ] A4: Add integration tests for create/get event and manifest snapshot structure

### Capability B: Event staffing

- [ ] B1: Implement AssignEventManager domain and application behavior
- [ ] B2: Deliver AssignEventManager endpoint with EventManager claim requirement
- [ ] B3: Add integration tests for success, unauthorized, and invalid/not-found paths

### Capability C: Publication and integration

- [ ] C1: Implement PublishEvent state transition guards, including no publish without manifest
- [ ] C2: Enforce manifest structural immutability after publication
- [ ] C3: Write EventPublished(EventId, ManifestId) to outbox on publication
- [ ] C4: Ensure outbox dispatcher processes EventPublished reliably
- [ ] C5: Add integration tests for outbox write and dispatch observability

## Out of Scope

- CancelEvent behavior and cancellation workflows
- Ticketing module synchronization behavior (starts in Milestone 03)
- Attendance and Identity module changes

## Definition of Done

- [ ] All in-scope capability issues are implemented and merged
- [ ] Integration tests are included in each implemented slice
- [ ] EventPublished outbox write and dispatch path is validated
- [ ] Architecture tests pass without new module-boundary violations
- [ ] Baseline CI remains green
- [ ] Milestone and issue docs are updated to reflect completion state

## Validation Checklist

- [ ] dotnet build passes at solution level
- [ ] dotnet test passes at solution level
- [ ] CreateEvent and GetEvent flow verified end-to-end
- [ ] AssignEventManager authorization behavior verified
- [ ] PublishEvent transition and outbox write verified
- [ ] Outbox dispatcher processing verified in integration scenario

## Risks and Dependencies

- Outbox dispatcher reliability and idempotency behavior may require additional hardening slices
- Event and manifest snapshot boundaries must stay stable to avoid Ticketing rework in Milestone 03
- Authorization claim model needs consistent local dev token setup for integration coverage
- Sequential solo delivery limits parallel execution and increases dependency sensitivity
