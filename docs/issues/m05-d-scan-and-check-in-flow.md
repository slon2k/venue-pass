# Capability D: Scan and check-in flow

## Summary

Deliver the operational scan endpoint and command flow that validates tickets in real time through Ticketing, records scan attempts, creates successful check-ins, rejects malformed/unknown/invalid/canceled/duplicate scans, and emits `TicketCheckedIn`.

## Scope

- In scope:
  - Deliver `ScanTicket` endpoint and handler
  - Normalize and validate incoming ticket code
  - Call Ticketing validation on every scan
  - Reject malformed, unknown, invalid, canceled, and duplicate scans
  - Fail closed when Ticketing validation is unavailable or times out
  - Create `AttendanceRecord` for accepted scans
  - Persist `ScanAttempt` for accepted and rejected scans
  - Emit `TicketCheckedIn` for successful check-ins
  - Enforce race-safe duplicate prevention with database uniqueness and conflict mapping
  - Ensure successful check-in and `TicketCheckedIn` outbox write are atomic
- Out of scope:
  - Offline scanning fallback
  - Multi-step admission workflows such as entry/exit
  - Fraud scoring and advanced anomaly detection
  - Projection-only ticket validation

## Functional Requirements

- [ ] SC-01 Valid issued ticket can be checked in successfully.
- [ ] SC-02 Successful scan creates exactly one attendance record.
- [ ] SC-03 Successful scan emits `TicketCheckedIn`.
- [ ] SC-04 Re-scan of same ticket is rejected as duplicate.
- [ ] SC-05 Duplicate prevention is owned by Attendance.
- [ ] SC-06 Duplicate prevention is enforced at database level.
- [ ] SC-07 Canceled ticket scan is rejected as invalid.
- [ ] SC-08 Unknown ticket scan is rejected as invalid or not found.
- [ ] SC-09 Every scan uses fresh Ticketing validation.
- [ ] SC-10 Scan attempts are persisted for accepted and rejected outcomes.
- [ ] SC-11 Rejected scan attempts include reason.
- [ ] SC-12 Attendance does not mutate Ticketing ticket status.
- [ ] SC-14 Ticketing validation failure or timeout fails closed with explicit `ValidationUnavailable` reason and creates no check-in.
- [ ] SC-15 Duplicate scan attempts must not emit duplicate `TicketCheckedIn` events.
- [ ] SC-16 Successful attendance record creation and `TicketCheckedIn` event write occur atomically.
- [ ] ER-01 Unknown ticket error contract is consistent.
- [ ] ER-02 Invalid or canceled ticket error contract is consistent.
- [ ] ER-03 Duplicate scan maps to conflict contract.
- [ ] ER-04 Concurrency race loser maps to deterministic conflict contract.
- [ ] ER-06 Ticketing validation timeout/unavailability returns a distinct operational failure response and is not reported as an invalid ticket.
- [ ] Malformed ticket code is rejected deterministically and creates no check-in.

## Endpoint Contract

### `POST /attendance/scans`

- Auth: operational role
- Request minimum:
  - ticket code

Success response includes:

- accepted outcome
- attendance record identity
- ticket ID
- ticket code
- event or published event reference
- checked-in timestamp

Failure categories:

- malformed ticket code
- unknown ticket
- invalid ticket
- canceled ticket
- duplicate scan
- validation dependency unavailable or timeout

Failure rules:

- malformed, unknown, invalid, canceled, duplicate, and validation-unavailable outcomes create no `AttendanceRecord`
- rejected outcomes are persisted as `ScanAttempt`
- validation dependency unavailable is not reported as invalid ticket

## Concurrency Strategy

- Use database unique constraint for one successful check-in per ticket.
- Use Ticketing validation result ticket ID as the canonical duplicate-prevention key.
- Handle unique-constraint race loser deterministically as duplicate conflict.
- Duplicate scan attempts do not emit duplicate `TicketCheckedIn` events.
- Successful `AttendanceRecord` creation and `TicketCheckedIn` outbox write occur in the same transaction.
- Command processing is retry-safe for transport or client retries:
  - no duplicate attendance records
  - no duplicate `TicketCheckedIn` events
  - deterministic duplicate/conflict response for already checked-in tickets

## Acceptance Criteria

- [ ] First valid scan succeeds and records check-in.
- [ ] First valid scan emits exactly one `TicketCheckedIn`.
- [ ] Second scan of same ticket is rejected as duplicate.
- [ ] Duplicate scan does not emit another `TicketCheckedIn`.
- [ ] Unknown ticket is rejected with deterministic reason.
- [ ] Malformed ticket code is rejected with deterministic reason.
- [ ] Canceled ticket is rejected with deterministic reason.
- [ ] Ticketing validation outage or timeout causes fail-closed operational failure and no check-in.
- [ ] Accepted and rejected scans are persisted as scan attempts.
- [ ] Concurrent scans of same ticket produce exactly one successful attendance record.
- [ ] Concurrency race loser maps to deterministic duplicate/conflict response.

## Test Checklist

- [ ] Integration test: valid scan succeeds.
- [ ] Integration test: valid scan creates attendance record.
- [ ] Integration test: valid scan writes `TicketCheckedIn` outbox message.
- [ ] Integration test: duplicate scan is rejected.
- [ ] Integration test: duplicate scan does not write duplicate `TicketCheckedIn`.
- [ ] Integration test: canceled scan is rejected.
- [ ] Integration test: unknown scan is rejected.
- [ ] Integration test: malformed ticket code is rejected.
- [ ] Integration test: rejected scans persist scan attempts with reason.
- [ ] Concurrency test: simultaneous scans produce one success only.
- [ ] Concurrency test: race loser is mapped to duplicate/conflict response.
- [ ] Resilience test: Ticketing validation timeout fails closed and creates no check-in.
- [ ] Resilience test: Ticketing validation unavailable is not reported as invalid ticket.
- [ ] Authorization test: scan endpoint requires operational role.
- [ ] Boundary test: scan flow calls Ticketing validation and does not rely solely on Attendance projection.

## Dependencies

- Capability A validation contract and cancellation semantics.
- Capability B persistence and uniqueness constraints.
- Capability C projection availability for contextual read support; projection must not be required for ticket-validity decisions.

## Risks

- High contention at venue gates can surface race-condition bugs quickly.
- Dependency latency or outages in Ticketing validation can degrade operator flow.
- Incorrect transaction boundaries can create check-ins without corresponding `TicketCheckedIn` events.
- Incorrect retry/concurrency handling can emit duplicate `TicketCheckedIn` events.
- Treating validation-unavailable as invalid-ticket can confuse operators and hide operational incidents.
