# D-03: Implement duplicate prevention, concurrency handling, and retry-safe atomicity

## Goal

Guarantee one successful check-in per ticket, deterministic duplicate behavior, and no duplicate outbox emissions under concurrency or retries.

## Scope

- Use `TicketId` from Ticketing validation result as canonical duplicate key.
- Enforce uniqueness at database level for successful attendance records by `TicketId`.
- Pre-check for existing attendance record before insert.
- Handle unique-constraint race loser deterministically as duplicate/conflict.
- Persist duplicate attempts as rejected scan attempts.
- Ensure duplicate attempts never emit `TicketCheckedIn`.
- Ensure successful attendance record and `TicketCheckedIn` outbox write are in the same transaction.
- Ensure retry-safe behavior for repeated client submissions.

## Acceptance Criteria

- [x] Second scan of same ticket is rejected as duplicate.
- [x] Duplicate scan persists rejected scan attempt with duplicate reason.
- [x] Duplicate scan does not create second attendance record.
- [x] Duplicate scan does not emit another `TicketCheckedIn`.
- [x] Concurrent scans of same ticket produce exactly one successful attendance record.
- [x] Concurrent scans of same ticket produce exactly one `TicketCheckedIn` outbox message.
- [x] Race loser returns deterministic duplicate/conflict response.
- [x] Race loser persists rejected scan attempt.
- [x] Successful attendance record and outbox write are atomic.

## Tests

- [x] Integration test: duplicate scan rejected as duplicate.
- [x] Integration test: duplicate scan persists rejected attempt with duplicate reason.
- [x] Integration test: duplicate scan does not write duplicate `TicketCheckedIn` outbox message.
- [x] Concurrency test: simultaneous scans produce exactly one success.
- [x] Concurrency test: race loser maps to duplicate/conflict deterministically.
- [x] Concurrency test: exactly one `TicketCheckedIn` outbox message exists after concurrent scans.
- [x] Transaction test: attendance record and outbox write are atomic.
