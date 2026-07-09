# D-01: Implement ScanTicket endpoint and successful check-in flow

## Goal

Deliver the happy path for operational scanning:

- `POST /attendance/scans` accepts a ticket code.
- Ticketing validation is called on every scan.
- A successful validation creates one attendance record.
- An accepted scan attempt is persisted.
- One `TicketCheckedIn` outbox message is written.
- Attendance record and outbox write are atomic.

## Scope

- Add request and response contract for `POST /attendance/scans`.
- Add endpoint authorization for operational role.
- Normalize and validate submitted ticket code before validation call.
- Call Ticketing validation on every scan.
- For valid result:
  - Persist accepted scan attempt.
  - Persist attendance record.
  - Persist `TicketCheckedIn` outbox message.
  - Commit as one transaction.

## Acceptance Criteria

- [ ] Valid issued ticket can be checked in.
- [ ] Successful scan creates exactly one attendance record.
- [ ] Successful scan creates exactly one accepted scan attempt.
- [ ] Successful scan writes exactly one `TicketCheckedIn` outbox message.
- [ ] Attendance does not mutate Ticketing ticket state.
- [ ] Scan flow calls Ticketing validation and does not rely only on Attendance projection.
- [ ] Endpoint requires operational role.
- [ ] Attendance record and `TicketCheckedIn` outbox message are persisted atomically.

## Tests

- [ ] Integration test: valid scan succeeds.
- [ ] Integration test: valid scan creates one attendance record.
- [ ] Integration test: valid scan creates one accepted scan attempt.
- [ ] Integration test: valid scan writes one `TicketCheckedIn` outbox message.
- [ ] Authorization test: endpoint requires operational role.
