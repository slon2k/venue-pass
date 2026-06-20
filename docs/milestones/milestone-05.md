# Milestone 05 - Attendance: Check-In

## Status

- Status: Planned
- Prerequisite milestone: M04 - Ticketing reservation, checkout, order creation, ticket issuance, and ticket retrieval
- Primary modules touched:
  - Ticketing
  - Attendance
- Roadmap adjustment:
  - M05 includes Ticketing readiness work required by Attendance:
    - ticket validation contract
    - ticket lifecycle integration events
    - minimal ticket cancellation

## Milestone Outcome

Attendance check-in is implemented end-to-end.

Event staff can scan a ticket code, Attendance validates ticket validity through Ticketing, records successful check-ins, rejects duplicates, and rejects invalid or canceled tickets.

Ticketing remains the source of truth for ticket validity. Attendance owns check-in records and duplicate-use prevention.

## In Scope

- [ ] Capability A: Ticketing readiness for Attendance
- [ ] Capability B: Attendance module foundation and persistence
- [ ] Capability C: Ticket lifecycle projection in Attendance
- [ ] Capability D: Scan/check-in flow
- [ ] Capability E: Attendance record retrieval
- [ ] Capability F: Integration, authorization, and concurrency tests

## Capability Breakdown

### Capability A: Ticketing readiness for Attendance

- [ ] A1: Add public Ticketing module contract for ticket validation:
  - `ITicketingModule.GetTicketForValidationAsync(...)`
- [ ] A2: Implement ticket validation query in Ticketing.
- [ ] A3: Ensure validation response includes data required by Attendance:
  - ticket ID
  - ticket code
  - ticket status
  - validity flag
  - order ID
  - order item ID
  - event or published event reference
  - inventory target reference (seat or GA)
- [ ] A4: Emit `TicketIssued` integration event on successful checkout.
- [ ] A5: Add `TicketStatus.Canceled`.
- [ ] A6: Add minimal ticket cancellation behavior in Ticketing.
- [ ] A7: Deliver `CancelTicket` endpoint and handler.
- [ ] A8: Emit `TicketCanceled` integration event when a ticket is canceled.
- [ ] A9: Ensure canceled tickets validate as invalid through Ticketing contract.

### Capability B: Attendance module foundation and persistence

- [ ] B1: Add Attendance module configuration.
- [ ] B2: Add `AttendanceDbContext`.
- [ ] B3: Add `attendance` schema and migrations.
- [ ] B4: Add Attendance outbox configuration when not already provided by shared infrastructure.
- [ ] B5: Implement Attendance domain model:
  - `AttendanceRecord`
  - `ScanAttempt`
- [ ] B6: Implement Attendance ticket projection/read model.
- [ ] B7: Add persistence configuration for:
  - attendance records
  - scan attempts
  - ticket projections
- [ ] B8: Add uniqueness constraints for duplicate prevention:
  - one successful check-in per ticket

### Capability C: Ticket lifecycle projection in Attendance

- [ ] C1: Subscribe Attendance to `TicketIssued`.
- [ ] C2: Handle `TicketIssued` by creating or updating local projection.
- [ ] C3: Subscribe Attendance to `TicketCanceled`.
- [ ] C4: Handle `TicketCanceled` by marking local projection canceled or invalid.
- [ ] C5: Make both consumers idempotent.
- [ ] C6: Treat projection as read model only, never source of truth for scan validity.

### Capability D: Scan/check-in flow

- [ ] D1: Deliver `ScanTicket` endpoint and handler.
- [ ] D2: Normalize and validate submitted ticket code.
- [ ] D3: Call Ticketing validation contract on every scan.
- [ ] D4: Reject unknown tickets.
- [ ] D5: Reject canceled or invalid tickets.
- [ ] D6: Reject duplicate scans for already checked-in tickets.
- [ ] D7: Create `AttendanceRecord` for accepted scans.
- [ ] D8: Persist `ScanAttempt` for accepted and rejected scans.
- [ ] D9: Emit `TicketCheckedIn` integration event after successful check-in.
- [ ] D10: Resolve duplicate-check races using database uniqueness and deterministic conflict mapping.

### Capability E: Attendance record retrieval

- [ ] E1: Deliver `GetAttendanceStatus` endpoint and handler.
- [ ] E2: Support lookup by ticket code or ticket ID.
- [ ] E3: Response includes:
  - ticket ID
  - ticket code
  - event or published event reference
  - check-in status
  - checked-in timestamp, if present
  - last rejection reason or scan metadata, if included
- [ ] E4: Return not found when no attendance record or projection exists for requested ticket.

### Capability F: Integration, authorization, and concurrency tests

- [ ] F1: Ticketing validation contract tests:
  - issued ticket validates as valid
  - canceled ticket validates as invalid
  - unknown ticket validates as invalid or not found
- [ ] F2: Ticketing event tests:
  - checkout writes `TicketIssued`
  - cancellation writes `TicketCanceled`
- [ ] F3: Attendance projection tests:
  - `TicketIssued` creates or updates projection
  - `TicketCanceled` marks projection invalid
  - duplicate event delivery is idempotent
- [ ] F4: Scan flow tests:
  - valid ticket scan succeeds
  - duplicate scan is rejected
  - canceled ticket scan is rejected
  - unknown ticket scan is rejected
- [ ] F5: Concurrency tests:
  - simultaneous scans of same valid ticket produce exactly one successful attendance record
- [ ] F6: End-to-end test:
  - publish event -> inventory -> offer -> reservation -> checkout -> ticket issued -> attendance scan
- [ ] F7: Authorization enforcement tests for new Ticketing and Attendance endpoints.
- [ ] F8: Migration smoke test includes Attendance schema.
- [ ] F9: Resilience test:
  - Ticketing validation timeout or transient failure rejects scan (fail closed)

## Functional Requirements Baseline (M05)

These requirements define minimum business behavior for M05 and are implementation gates.

### Ticketing Readiness Requirements (TR)

- [ ] TR-01: Ticketing exposes a public module contract for ticket validation.
- [ ] TR-02: Attendance consumes Ticketing through public contract only.
- [ ] TR-03: Attendance does not depend on Ticketing internals.
- [ ] TR-04: Ticket validation accepts ticket code input.
- [ ] TR-05: Ticket validation normalizes ticket code input using Ticketing rules.
- [ ] TR-06: Unknown ticket codes return invalid or not-found validation result.
- [ ] TR-07: Issued tickets return valid validation result.
- [ ] TR-08: Canceled tickets return invalid validation result.
- [ ] TR-09: Validation response includes current ticket status.
- [ ] TR-10: Validation response includes stable identifiers required by Attendance:
  - ticket ID
  - ticket code
  - order ID
  - order item ID
  - event or published event reference
  - inventory target reference
- [ ] TR-11: Ticketing remains source of truth for ticket validity.

### Ticket Lifecycle Event Requirements (EV)

- [ ] EV-01: Successful checkout emits one `TicketIssued` event per issued ticket.
- [ ] EV-02: `TicketIssued` includes:
  - ticket ID
  - ticket code
  - order ID
  - order item ID
  - event or published event reference
  - inventory target reference
  - issued timestamp
- [ ] EV-03: Ticketing supports minimal cancellation of issued tickets.
- [ ] EV-04: Canceling a ticket changes status from Issued to Canceled.
- [ ] EV-05: Canceling a ticket emits `TicketCanceled`.
- [ ] EV-06: `TicketCanceled` includes:
  - ticket ID
  - ticket code
  - event or published event reference
  - canceled timestamp
- [ ] EV-07: Repeated cancellation is deterministic and idempotent.
- [ ] EV-08: Cancellation in M05 does not include refunds, order cancellation, inventory release, or payment reversal.
- [ ] EV-09: Repeated/idempotent checkout does not emit duplicate `TicketIssued` events for tickets already issued.
- [ ] EV-10: Repeated/idempotent cancellation does not emit duplicate `TicketCanceled` events for tickets already canceled.

### Attendance Projection Requirements (AP)

- [ ] AP-01: Attendance receives `TicketIssued`.
- [ ] AP-02: Attendance stores or updates local ticket projection.
- [ ] AP-03: Attendance receives `TicketCanceled`.
- [ ] AP-04: Attendance marks local projection canceled or invalid.
- [ ] AP-05: Projection handlers are idempotent by ticket ID.
- [ ] AP-06: Projection handlers are idempotent by ticket code.
- [ ] AP-07: Projection is not authoritative for scan validity.
- [ ] AP-08: Scan flow still calls Ticketing validation on every scan.

### Scan and Check-In Requirements (SC)

- [ ] SC-01: A valid issued ticket can be checked in successfully.
- [ ] SC-02: Successful scan creates exactly one attendance record.
- [ ] SC-03: Successful scan emits `TicketCheckedIn`.
- [ ] SC-04: Scanning same valid ticket again is rejected as duplicate.
- [ ] SC-05: Duplicate prevention is owned by Attendance.
- [ ] SC-06: Duplicate prevention is enforced at database level.
- [ ] SC-07: Scanning canceled ticket is rejected as invalid.
- [ ] SC-08: Scanning unknown ticket is rejected as invalid or not found.
- [ ] SC-09: Scan flow uses fresh Ticketing validation on every scan.
- [ ] SC-10: Scan attempts are recorded for accepted and rejected scans.
- [ ] SC-11: Rejected scan attempts include reason.
- [ ] SC-12: Attendance does not change Ticketing ticket status during check-in.
- [ ] SC-13: Ticketing does not store check-in state in M05.
- [ ] SC-14: If Ticketing validation fails or times out, scan fails closed with explicit `ValidationUnavailable` reason and no check-in is created.
- [ ] SC-15: Duplicate scan attempts must not emit duplicate `TicketCheckedIn` events.
- [ ] SC-16: Successful attendance record creation and `TicketCheckedIn` event write occur atomically.

### Attendance Record Requirements (AR)

- [ ] AR-01: Attendance record stores:
  - attendance record ID
  - ticket ID
  - ticket code
  - event or published event reference
  - checked-in timestamp
- [ ] AR-02: Attendance record may store:
  - order ID
  - order item ID
  - inventory seat ID
  - GA pool ID
- [ ] AR-03: A ticket has at most one successful attendance record.
- [ ] AR-04: Attendance retrieval returns check-in status for known ticket.
- [ ] AR-05: Attendance retrieval reports Attendance state only and does not perform ticket validity checks.

### Authorization Requirements (AU)

- [ ] AU-01: Ticketing customer-facing M04 endpoints remain authenticated.
- [ ] AU-02: `CancelTicket` requires operational role.
- [ ] AU-03: Attendance scan endpoint requires operational role.
- [ ] AU-04: Attendance retrieval endpoint requires operational role.
- [ ] AU-05: M05 may continue using dev JWT role claims until Identity in M06.
- [ ] AU-06: Recommended M05 operational policy:
  - `AttendanceOperator`
  - allowed roles: `EventAdmin`, `EventManager`

### API Error Contract Requirements (ER)

- [ ] ER-01: Unknown ticket scan returns consistent not-found or invalid response contract.
- [ ] ER-02: Canceled or invalid ticket scan returns consistent invalid response contract.
- [ ] ER-03: Duplicate scan returns conflict response contract.
- [ ] ER-04: Concurrency race loser returns deterministic conflict response contract.
- [ ] ER-05: Unauthorized or forbidden access maps consistently to authentication and authorization response contracts.
- [ ] ER-06: Ticketing validation timeout/unavailability returns a distinct operational failure response and is not reported as an invalid ticket.

### Observability Requirements (OP)

- [ ] OP-01: Scan attempts are logged with outcome and reason category.
- [ ] OP-02: Ticketing validation failures/timeouts are logged with reason category.

## Accepted Decisions (Locked For M05)

1. M05 starts with Ticketing readiness work required by Attendance, even if originally planned in M04.
2. Ticketing is authoritative for ticket validity; Attendance projection is read model only.
3. Scan flow validates against Ticketing on every scan and never allows projection-only acceptance.
4. Scan flow fails closed when Ticketing validation cannot be completed.
5. Attendance owns duplicate prevention and persists exactly one successful check-in per ticket.
6. Duplicate prevention is enforced with database uniqueness plus conflict-safe command handling.
7. Ticket cancellation in M05 is intentionally minimal and limited to Issued -> Canceled behavior.
8. Repeated cancellation is treated idempotently.
9. Cancellation does not trigger refund, order cancellation, inventory release, or payment reversal in M05.
10. M05 uses operational roles from dev JWT claims until Identity milestone is delivered.
11. M05 does not include offline scan mode.
12. M05 does not include check-out or exit workflows.

## Slice Start Gate

- [ ] M04 ticket issuance and ticket retrieval flows remain green in CI.
- [ ] Cross-module contract shape for ticket validation is approved.
- [ ] Integration event contracts (`TicketIssued`, `TicketCanceled`, `TicketCheckedIn`) are reviewed and documented.
- [ ] Attendance persistence strategy and migration naming convention are agreed.

## Out of Scope

- Refunds and payment reversal behavior
- Full order cancellation workflows
- Inventory re-allocation after cancellation
- Ticket transfer or ticket re-issuance
- Offline scanning mode
- Entry and exit lifecycle beyond initial check-in
- Attendance analytics dashboards or reporting UI
- Identity redesign (deferred to M06)

## Definition of Done

- [ ] All in-scope capabilities are implemented and merged.
- [ ] M05 functional requirements baseline is satisfied.
- [ ] Integration tests validate cross-module check-in flow and event idempotency.
- [ ] Concurrency tests prove single successful check-in under simultaneous scans.
- [ ] Authorization tests pass for all new M05 endpoints.
- [ ] Architecture tests pass without new boundary violations.
- [ ] CI remains green.
- [ ] Milestone and roadmap docs are updated to reflect completion status.

## Validation Checklist

- [ ] `dotnet build` passes at solution level.
- [ ] `dotnet test` passes at solution level.
- [ ] Issued ticket validates as valid via Ticketing contract.
- [ ] Canceled ticket validates as invalid via Ticketing contract.
- [ ] Unknown ticket validates as invalid or not found via Ticketing contract.
- [ ] Checkout emits `TicketIssued` once per issued ticket.
- [ ] Cancel operation emits `TicketCanceled` for canceled ticket.
- [ ] Attendance projection is created or updated on `TicketIssued`.
- [ ] Attendance projection is marked invalid on `TicketCanceled`.
- [ ] Replayed `TicketIssued` and `TicketCanceled` messages are idempotent.
- [ ] Valid issued ticket scan succeeds and creates one attendance record.
- [ ] Re-scan of checked-in ticket is rejected as duplicate.
- [ ] Canceled ticket scan is rejected.
- [ ] Unknown ticket scan is rejected.
- [ ] Simultaneous scans for same ticket produce exactly one successful check-in.
- [ ] Scan while Ticketing validation is unavailable fails closed and does not create check-in.
- [ ] `GetAttendanceRecord` returns expected check-in status fields for known ticket.
- [ ] `GetAttendanceRecord` returns not found for unknown ticket.
- [ ] Authorization is enforced for `CancelTicket`, `ScanTicket`, and attendance retrieval endpoints.
- [ ] Attendance schema migration is applied successfully in integration environment.

## Risks and Dependencies

- Concurrency correctness for duplicate prevention under real scan contention is the highest technical risk.
- Cross-module contract drift between Ticketing and Attendance can introduce integration failures.
- Event ordering or delayed delivery may expose projection consistency gaps; handlers must remain idempotent.
- Validation call reliability directly impacts scan throughput and operator experience.
- M05 depends on M04 stability for reservation, checkout, order creation, and ticket issuance invariants.
