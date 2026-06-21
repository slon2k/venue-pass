# Capability F: Integration, authorization, and concurrency tests

## Summary

Add comprehensive automated test coverage for M05 cross-module contracts, event projection, scan correctness, authorization, migration safety, resilience behavior, and concurrency/idempotency guarantees.

This capability verifies the full M05 milestone baseline across Ticketing and Attendance.

## Scope

- In scope:
  - Ticketing validation contract tests
  - Ticket lifecycle event emission and idempotency tests
  - Attendance projection idempotency and ordering tests
  - Scan/check-in success and rejection tests
  - Duplicate prevention and concurrency tests
  - `TicketCheckedIn` event emission tests
  - Attendance status retrieval tests
  - End-to-end milestone flow test
  - Authorization tests for new Ticketing and Attendance endpoints
  - Attendance migration smoke test
  - Resilience test for Ticketing validation timeout or transient failure
- Out of scope:
  - Performance or load testing at production scale
  - Full chaos engineering suite
  - UI end-to-end tests
  - Manual operational runbooks

## Required Coverage

### F1: Ticketing validation contract

- [ ] Issued ticket validates as valid.
- [ ] Canceled ticket validates as invalid.
- [ ] Unknown ticket validates as invalid or not found.
- [ ] Malformed ticket code validates as malformed/invalid without unhandled exception.
- [ ] Validation response includes current ticket status.
- [ ] Validation response includes required stable identifiers:
  - ticket ID
  - ticket code
  - order ID
  - order item ID
  - canonical event reference
  - inventory target reference
- [ ] Validation contract returns DTO/contract types only, not Ticketing domain entities.

### F2: Ticket lifecycle event emission

- [ ] Checkout writes one `TicketIssued` event per newly issued ticket.
- [ ] Repeated/idempotent checkout does not write duplicate `TicketIssued` events for already-issued tickets.
- [ ] Cancellation writes `TicketCanceled`.
- [ ] Repeated/idempotent cancellation does not write duplicate `TicketCanceled` events for already-canceled tickets.
- [ ] `TicketIssued` payload includes required identifiers and timestamp.
- [ ] `TicketCanceled` payload includes required identifiers and timestamp.
- [ ] Event payloads use the canonical event reference consistently.
- [ ] Ticket cancellation status update and `TicketCanceled` outbox write are persisted atomically.

### F3: Attendance projection behavior

- [ ] `TicketIssued` creates projection.
- [ ] `TicketIssued` updates projection when already present.
- [ ] Replayed `TicketIssued` remains idempotent.
- [ ] `TicketCanceled` marks projection canceled/invalid.
- [ ] Replayed `TicketCanceled` remains idempotent.
- [ ] `TicketCanceled` arriving before `TicketIssued` creates or keeps canceled projection.
- [ ] Replayed or delayed `TicketIssued` does not revert canceled projection to issued.
- [ ] Concurrent duplicate event handling does not create duplicate projections.
- [ ] Projection uses canonical event reference.
- [ ] Projection stores normalized ticket code.

### F4: Scan/check-in behavior

- [ ] Valid issued ticket scan succeeds.
- [ ] Valid scan creates one attendance record.
- [ ] Valid scan persists accepted `ScanAttempt`.
- [ ] Valid scan writes one `TicketCheckedIn` outbox message.
- [ ] Duplicate scan is rejected.
- [ ] Duplicate scan persists rejected `ScanAttempt` with reason.
- [ ] Duplicate scan does not emit duplicate `TicketCheckedIn`.
- [ ] Canceled ticket scan is rejected.
- [ ] Unknown ticket scan is rejected.
- [ ] Malformed ticket code scan is rejected.
- [ ] Rejected scans persist `ScanAttempt` with reason category.
- [ ] Attendance does not mutate Ticketing ticket status during scan.
- [ ] Scan flow calls Ticketing validation and does not rely solely on Attendance projection.

### F5: Concurrency and duplicate prevention

- [ ] Simultaneous scans of the same valid ticket produce exactly one successful attendance record.
- [ ] Simultaneous scans of the same valid ticket produce exactly one `TicketCheckedIn` event.
- [ ] Race loser maps to deterministic duplicate/conflict response.
- [ ] Database uniqueness prevents duplicate successful check-ins.
- [ ] Retry/client repeat behavior does not create duplicate attendance records.
- [ ] Retry/client repeat behavior does not create duplicate `TicketCheckedIn` events.

### F6: Attendance status retrieval

- [ ] Lookup by ticket code returns known projected ticket.
- [ ] Lookup by ticket ID returns known projected ticket.
- [ ] Projected ticket without attendance record returns `NotCheckedIn`.
- [ ] Checked-in ticket returns `CheckedIn` with attendance metadata.
- [ ] Canceled projected ticket can be retrieved without Ticketing validation.
- [ ] Unknown ticket returns not found.
- [ ] Rejected scan attempt alone does not make unknown ticket retrievable as known.
- [ ] Ticket code lookup normalizes input.
- [ ] Retrieval path does not invoke Ticketing validation.
- [ ] Retrieval path does not create scan attempts.
- [ ] Retrieval path does not mutate Attendance state.

### F7: End-to-end milestone flow

- [ ] Publish event.
- [ ] Ticketing creates inventory.
- [ ] Create offer.
- [ ] Configure pricing.
- [ ] Create reservation.
- [ ] Checkout reservation.
- [ ] Tickets are issued.
- [ ] `TicketIssued` is produced.
- [ ] Attendance consumes `TicketIssued`.
- [ ] Scan issued ticket.
- [ ] Attendance record is created.
- [ ] `TicketCheckedIn` is produced.
- [ ] Duplicate scan is rejected.
- [ ] Cancel another issued ticket.
- [ ] `TicketCanceled` is produced.
- [ ] Attendance consumes `TicketCanceled`.
- [ ] Scanning canceled ticket is rejected.

### F8: Authorization

- [ ] Unauthenticated `CancelTicket` request is rejected.
- [ ] Unauthorized role cannot cancel tickets.
- [ ] Authorized operational role can cancel tickets.
- [ ] Unauthenticated scan request is rejected.
- [ ] Unauthorized role cannot scan tickets.
- [ ] Authorized operational role can scan tickets.
- [ ] Unauthenticated attendance retrieval request is rejected.
- [ ] Unauthorized role cannot retrieve attendance status.
- [ ] Authorized operational role can retrieve attendance status.
- [ ] Existing M04 customer-facing Ticketing endpoints remain authenticated according to M04 baseline.

### F9: Migration smoke

- [ ] Attendance schema migration applies cleanly in test environment.
- [ ] Attendance tables are created under the expected schema.
- [ ] Required uniqueness constraints exist.
- [ ] Required lookup indexes exist.
- [ ] Migration smoke test runs from a clean database.

### F10: Resilience

- [ ] Ticketing validation timeout fails closed.
- [ ] Ticketing validation transient failure fails closed.
- [ ] Validation unavailable response is distinct from invalid-ticket response.
- [ ] Validation unavailable creates no attendance record.
- [ ] Validation unavailable persists rejected scan attempt with `ValidationUnavailable` reason.

### F11: Architecture and module boundaries

- [ ] Attendance consumes Ticketing through public contracts/events only.
- [ ] Attendance does not reference Ticketing internals.
- [ ] Ticket validation contract does not expose Ticketing domain entities.
- [ ] Architecture tests pass without new module-boundary violations.

## Test Design Notes

- Prefer integration tests for cross-module behavior and contract fidelity.
- Use unit tests only where they provide clear value for deterministic branch behavior.
- Use deterministic builders for:
  - published events
  - inventory
  - offers
  - reservations
  - orders
  - tickets
  - ticket lifecycle events
  - attendance records
- Keep concurrency tests repeatable with:
  - bounded parallelism
  - explicit synchronization/barriers where useful
  - clear assertions against database state and outbox state
- Assert idempotency under replay for:
  - `TicketIssued`
  - `TicketCanceled`
  - duplicate scan attempts
  - repeated checkout
  - repeated cancellation
- Avoid sleeps/timing-sensitive assertions where possible.
- Tests should verify externally observable behavior rather than implementation details.
- Test names should map clearly to M05 requirement IDs or capability IDs where practical.

## Acceptance Criteria

- [ ] All M05 critical behaviors are covered by automated tests.
- [ ] Validation contract regressions are detectable in CI.
- [ ] Ticket lifecycle event regressions are detectable in CI.
- [ ] Projection idempotency and ordering regressions are detectable in CI.
- [ ] Scan/check-in correctness regressions are detectable in CI.
- [ ] Duplicate check-in concurrency regressions are detectable in CI.
- [ ] Authorization regressions on new endpoints are detectable in CI.
- [ ] Attendance retrieval regressions are detectable in CI.
- [ ] Migration safety for Attendance schema is validated automatically.
- [ ] Resilience behavior for validation timeout/unavailability is covered.
- [ ] Architecture boundary regressions are detectable in CI.

## Definition of Done for This Capability

- [ ] New tests are implemented, merged, and passing in CI.
- [ ] Existing M04 tests remain green.
- [ ] M05 test names map clearly to requirement IDs or capability behavior.
- [ ] Concurrency tests pass repeatedly in local and CI runs.
- [ ] Test fixtures/builders are reusable across M05 test suites.
- [ ] Flaky test rate remains acceptable after repeated local and CI runs.
- [ ] CI runtime remains acceptable for the expanded integration test suite.

## Dependencies

- Capability A: Ticketing validation contract, cancellation semantics, and lifecycle events.
- Capability B: Attendance persistence, uniqueness constraints, indexes, and migrations.
- Capability C: Attendance projection consumers.
- Capability D: Scan/check-in flow.
- Capability E: Attendance status retrieval.
- Stable integration-test infrastructure in `tests/`.
- Test database infrastructure capable of running migration and concurrency scenarios.

## Risks

- Non-deterministic concurrency tests can create false positives or negatives.
- Missing test fixtures for cross-module setup can slow delivery and reduce confidence.
- Overly broad end-to-end tests can become slow or brittle.
- Mocking Ticketing too heavily can hide cross-module contract drift.
- Timing-based resilience tests can become flaky if timeout behavior is not controlled.
