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

- [ ] Second scan of same ticket is rejected as duplicate.
- [ ] Duplicate scan persists rejected scan attempt with duplicate reason.
- [ ] Duplicate scan does not create second attendance record.
- [ ] Duplicate scan does not emit another `TicketCheckedIn`.
- [ ] Concurrent scans of same ticket produce exactly one successful attendance record.
- [ ] Concurrent scans of same ticket produce exactly one `TicketCheckedIn` outbox message.
- [ ] Race loser returns deterministic duplicate/conflict response.
- [ ] Race loser persists rejected scan attempt.
- [ ] Successful attendance record and outbox write are atomic.

## Tests

- [ ] Integration test: duplicate scan rejected as duplicate.
- [ ] Integration test: duplicate scan persists rejected attempt with duplicate reason.
- [ ] Integration test: duplicate scan does not write duplicate `TicketCheckedIn` outbox message.
- [ ] Concurrency test: simultaneous scans produce exactly one success.
- [ ] Concurrency test: race loser maps to duplicate/conflict deterministically.
- [ ] Concurrency test: exactly one `TicketCheckedIn` outbox message exists after concurrent scans.
- [ ] Transaction test: attendance record and outbox write are atomic.
