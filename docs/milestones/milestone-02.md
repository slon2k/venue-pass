# Milestone 02 - Events: Event Creation and Publication

## Milestone Outcome

Event lifecycle and publication flow are operational end-to-end, including outbox event emission and dispatch.

## Delivery Model

Milestone 02 is delivered through parent feature (capability) issues and vertical-slice sub-issues. Each slice includes domain behavior, persistence impact, endpoint behavior, and integration tests in the same PR.

## In Scope

- [x] Capability A: Event lifecycle foundation
- [ ] Capability B: Event staffing
- [ ] Capability C: Publication and integration

## Capability Breakdown

### Capability A: Event lifecycle foundation

- [x] A1: Implement Event aggregate lifecycle core for Draft and Published states
- [x] A2: Model and persist Manifest snapshot copied from selected ManifestTemplate
- [x] A3: Deliver CreateEvent and GetEvent end-to-end API behavior
- [x] A4: Add integration tests for create/get event and manifest snapshot structure

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

## Functional Requirements Baseline (M02)

These requirements define minimum business behavior for M02 and should be treated as implementation gates for slices A1-A3.

## Accepted Decisions (Locked For M02)

1. `ManifestTemplateId` is required in `CreateEvent`.
2. Attach/replace manifest flow is deferred (out of scope for M02).
3. `CreateEvent` uses minimal required fields: `Name`, `EventDateUtc`, `VenueId`, `ManifestTemplateId`.
4. Publication preconditions are strict minimal checks:
	- event is in `Draft`
	- manifest snapshot exists
	- current UTC time is strictly less than `EventDateUtc`
	- referenced venue remains valid
5. Error classification rule:
	- validation errors for missing/format/range/reference checks at the command boundary
	- domain errors for invalid state transitions and invariant violations inside aggregate/domain model

### Event Creation Requirements

- [x] Event has required `Name` and required `EventDateUtc`.
- [x] `EventDateUtc` must be strictly in the future at creation time.
- [x] `VenueId` is required at creation and must reference an existing venue.
- [x] Event starts in `Draft` state.
- [x] `CreateEvent` rejects past or invalid dates, missing required fields, or unknown references with validation/domain errors.

### Manifest Attachment Requirements

- [x] `ManifestTemplateId` rule is explicit: required at creation for M02.
- [x] Attach/replace manifest flow is deferred to a later milestone.
- [x] Since manifest is required at creation, snapshot must be copied during creation.
- [x] Snapshot is independent from later template edits.

### Lifecycle and Publication Requirements

- [x] Allowed transition in M02: `Draft -> Published`.
- [x] Publication preconditions are explicit and testable.
- [x] Publication is rejected when current UTC time is greater than or equal to `EventDateUtc`.
- [x] Structural manifest edits are rejected after publication.
- [x] Successful publication writes `EventPublished(EventId, ManifestId)` to outbox.

### API Contract Requirements

- [x] `CreateEvent` request/response contract baseline is fixed for M02 (minimal required fields).
- [x] `GetEvent` response includes lifecycle state and event date fields.
- [x] Date-time representation is standardized as UTC in contracts.

## Decision Log (Must Be Closed Before Slice A3)

- [x] D1: `ManifestTemplateId` is required in `CreateEvent`.
- [x] D2: Attach/replace manifest is deferred (not in M02).
- [x] D3: Final field set for event creation is minimal required set.
- [x] D4: Publication preconditions checklist is fixed and explicit.
- [x] D5: Error classification matrix for validation vs domain rule violations is fixed.

## Slice Start Gate

- [x] Functional requirements above reviewed and accepted.
- [x] Decisions D1-D5 resolved and documented.
- [x] Feature A acceptance criteria and tests aligned with the decisions.

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

- [x] dotnet build passes at solution level
- [x] dotnet test passes at solution level
- [x] CreateEvent and GetEvent flow verified end-to-end
- [ ] AssignEventManager authorization behavior verified
- [ ] PublishEvent transition and outbox write verified
- [ ] Outbox dispatcher processing verified in integration scenario

## Risks and Dependencies

- Outbox dispatcher reliability and idempotency behavior may require additional hardening slices
- Event and manifest snapshot boundaries must stay stable to avoid Ticketing rework in Milestone 03
- Authorization claim model needs consistent local dev token setup for integration coverage
- Sequential solo delivery limits parallel execution and increases dependency sensitivity
