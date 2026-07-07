# Capability C: Ticket lifecycle projection in Attendance

## Summary

Implement Attendance-side projection consumers for `TicketIssued` and `TicketCanceled` so Attendance can maintain a local read model of ticket lifecycle state while still deferring validity decisions to Ticketing.

## Scope

- In scope:
  - Subscribe Attendance to `TicketIssued`
  - Upsert ticket projection on `TicketIssued`
  - Subscribe Attendance to `TicketCanceled`
  - Mark ticket projection canceled or invalid on `TicketCanceled`
  - Ensure idempotency for duplicate event deliveries
  - Ensure projection converges correctly under out-of-order event delivery
  - Preserve invariant that projection is read model only
- Out of scope:
  - Replacing Ticketing validation calls with projection-only checks
  - Projection-based authorization logic
  - Full scan/check-in command handling
  - Analytics aggregations

## Functional Requirements

- [x] AP-01 Attendance receives `TicketIssued`
- [x] AP-02 `TicketIssued` stores or updates local projection
- [x] AP-03 Attendance receives `TicketCanceled`
- [x] AP-04 `TicketCanceled` marks projection canceled or invalid
- [x] AP-05 Projection handlers are idempotent by ticket ID
- [x] AP-06 Projection handlers are idempotent by ticket code
- [x] AP-07 Projection is not authoritative for scan validity
- [x] AP-08 Scan flow still calls Ticketing validation on every scan

## Consumer and Idempotency Notes

- Consumers must support at-least-once delivery semantics.
- Message replay must not create duplicate projection rows.
- Projection updates must be safe under concurrent consumer execution.
- Projection uses the canonical event reference defined by Capability A.
- Projection stores normalized ticket code consistently.
- `Canceled` is terminal for the projection and must not be downgraded or reverted by a later/replayed `TicketIssued`.
- If `TicketCanceled` arrives before `TicketIssued`, the handler creates or updates a canceled projection using available event data.
- A later `TicketIssued` may fill missing projection fields but must not change projection status from canceled/invalid back to issued.
- Conflicting ticket ID / ticket code combinations are handled deterministically and logged as integration contract violations.

## Acceptance Criteria

- [x] `TicketIssued` creates projection when missing.
- [x] `TicketIssued` updates projection when already present.
- [x] `TicketCanceled` marks projection invalid.
- [x] Duplicate `TicketIssued` deliveries are idempotent.
- [x] Duplicate `TicketCanceled` deliveries are idempotent.
- [x] Out-of-order `TicketCanceled` before `TicketIssued` converges to canceled/invalid projection state.
- [x] Replayed or delayed `TicketIssued` does not revert canceled projection to issued.
- [x] Projection consumers do not expose projection as authoritative ticket-validity source.
- [x] Architecture tests still ensure Attendance does not depend on Ticketing internals.

## Test Checklist

- [x] Integration test: `TicketIssued` creates projection.
- [x] Integration test: replayed `TicketIssued` remains idempotent.
- [x] Integration test: `TicketCanceled` invalidates projection.
- [x] Integration test: replayed `TicketCanceled` remains idempotent.
- [x] Integration test: `TicketCanceled` arriving before `TicketIssued` creates or keeps canceled projection.
- [x] Integration test: replayed/delayed `TicketIssued` does not revert canceled projection to issued.
- [x] Integration test: concurrent duplicate event handling does not create duplicate projections.
- [x] Architecture test: Attendance consumes Ticketing events/contracts only and does not depend on Ticketing internals.

## Dependencies

- Capability A event contracts and outbox delivery
- Capability A canonical event reference decision
- Capability B persistence model, uniqueness constraints, and indexes

## Risks

- Event ordering edge cases can produce stale projection state if handlers are not convergence-safe.
- Missing idempotency guards can produce duplicate projections and nondeterministic reads.
- Replayed `TicketIssued` events could incorrectly reactivate canceled projections if status precedence is not enforced.
- Contract mismatches between ticket ID and ticket code can corrupt projection state if not handled deterministically.

## Related Issues

- Issue 150: Attendance TicketIssued Projection Consumer
- Issue 151: Attendance TicketCanceled Projection Consumer
- Issue 152: Projection Convergence And Boundary Tests
