# #42 — Add integration tests for outbox write and dispatch observability

## Summary

Add end-to-end integration tests for publication and outbox flow so we can
observe that publishing an event persists the expected outbox row and that the
outbox dispatcher processes rows reliably in a real database-backed test run.

## Scope

- In scope:
  - Publish endpoint integration tests for success and key guardrails
  - Outbox row correctness assertions after publication
  - Dispatcher integration tests proving processing, payload delivery, and retry
  - Test-only fixtures/helpers required to seed publishable entities and inspect
    persisted state
- Out of scope:
  - Ticketing subscriber behavior and cross-module synchronization
  - New retry policies (dead-letter, exponential backoff)
  - Production code behavior changes beyond deterministic testability hooks

## Prioritization

### Must-have for this PR

- Publication endpoint:
  - Publish valid draft -> `200 OK`, event state becomes `Published`
  - Publish valid draft -> manifest is frozen (`IsFrozen = true`)
  - Publish valid draft -> exactly one outbox row is written
  - Publish already-published event -> rejected (`409 Conflict`), state unchanged
  - Publish with no auth token -> `401`
  - Publish with wrong role -> `403`
  - Publish with caller not assigned manager -> `403`
- Outbox message correctness:
  - Type is assembly-qualified `EventPublishedIntegrationEvent`
  - Payload deserializes and contains matching `EventId` and `ManifestId`
  - Message is immediately eligible (`ProcessedOn = null`, `NextAttemptOn <= OccurredOn`)
- Dispatcher end-to-end:
  - Eligible message gets processed (`ProcessedOn` is set)
  - Registered handler receives deserialized payload with matching IDs
  - Handler failure does not process message and records retry metadata

### Next PR (recommended follow-up)

- Publish rejected when event date is in the past
- Publish rejected when venue no longer exists
- Dispatcher skips ineligible message (`NextAttemptOn > now`)
- Dispatcher marks message processed when no handler is registered
- Dispatcher poison-pill behavior (unresolvable type / invalid payload)
- Dispatcher failure isolation in a mixed batch (one fails, others still commit)
- Max-attempt abandonment integration assertion

## Acceptance Criteria

- [x] Integration tests cover publish success path and persistence side effects
      (event state, manifest freeze, outbox write)
- [x] Integration tests cover publish authorization failures (`401`/`403`) and
      already-published guard
- [x] Integration tests verify outbox row type and payload correctness
- [x] Integration tests verify dispatcher success and retry-on-failure behavior
      using a real database-backed test setup
- [x] Test names and assertions are deterministic and do not rely on brittle
      timing assumptions
- [x] `dotnet build` and `dotnet test` pass

## Vertical Slices

- [x] C5.1: Add publish endpoint success + authorization + already-published
      integration coverage
- [x] C5.2: Add outbox row correctness assertions (type, payload, eligibility)
- [x] C5.3: Add dispatcher success path integration coverage
- [x] C5.4: Add dispatcher failure/retry integration coverage
- [x] C5.5: Add test fixtures/helpers for deterministic setup and eventual
      assertions

## Design Notes

### Test structure

- Place publication tests under `Features/PublishEvent/` in
  `VenuePass.Modules.Events.IntegrationTests`.
- Keep one assertion focus per test method; use shared arrange helpers for
  creating venue/template/event and preparing auth context.

### Dispatcher observability strategy

- Prefer observing real hosted dispatcher behavior via eventual assertions with a
  bounded timeout and short polling interval in test assertions.
- If needed for determinism/speed, add a minimal test-only hook to execute one
  dispatch batch directly without introducing production behavior changes.

### Verification details

- Publish success should assert all three persisted outcomes together:
  event state, manifest freeze, and outbox row write.
- Dispatcher failure path should assert at minimum:
  `ProcessedOn == null`, `AttemptCount` incremented, `LastAttemptedOn` set,
  `NextAttemptOn` advanced, and `Error` populated.

## Risks and Assumptions

- Hosted background timing can introduce flaky tests if assertions are not
  eventually-consistent with a bounded timeout.
- Already-published publish attempts are mapped to `409 Conflict` via
  `ErrorType.Conflict` HTTP mapping.
- Integration tests are expected to validate wiring and persistence behavior;
  exhaustive edge matrices remain in unit tests.

## Definition of Done

- [x] Acceptance criteria met
- [x] Tests passing
- [x] Docs updated if behavior changed
