# D-02: Implement rejected scan outcomes and deterministic error mapping

## Goal

Handle all non-successful scans deterministically, fail closed on dependency failures, and persist rejected attempts.

## Scope

- Handle these outcomes:
  - malformed ticket code
  - unknown ticket
  - invalid ticket
  - canceled ticket
  - Ticketing validation timeout
  - Ticketing validation unavailable
- For all rejected outcomes:
  - No attendance record.
  - No `TicketCheckedIn` outbox message.
  - Persist rejected scan attempt with reason.
  - Return deterministic response contract.

## Mandatory Preparatory Requirement

- `ScanAttempt` must support raw/unparseable input persistence.
- If current model requires a valid `TicketCode` value object, extend model to store both raw input and optional normalized value.
- This is required so malformed scans are persisted as rejected attempts.

## Deterministic Response Mapping

- malformed ticket code: `400 Bad Request`, reason `MalformedTicketCode`
- unknown ticket: `404 Not Found`, reason `UnknownTicket`
- invalid ticket: `400 Bad Request`, reason `InvalidTicket`
- canceled ticket: `409 Conflict`, reason `CanceledTicket`
- validation timeout: `503 Service Unavailable`, reason `ValidationUnavailable`
- validation unavailable: `503 Service Unavailable`, reason `ValidationUnavailable`

## Rules

- Validation unavailable and timeout must never be reported as invalid ticket.
- All rejected outcomes persist a rejected scan attempt with the mapped reason.
- No rejected outcome creates attendance record or `TicketCheckedIn` outbox message.

## Acceptance Criteria

- [ ] Malformed ticket code is rejected deterministically and scan attempt is persisted.
- [ ] Unknown ticket is rejected with consistent contract and scan attempt is persisted.
- [ ] Invalid ticket is rejected with consistent contract and scan attempt is persisted.
- [ ] Canceled ticket is rejected with consistent contract and scan attempt is persisted.
- [ ] Validation timeout fails closed and persists rejected attempt with `ValidationUnavailable`.
- [ ] Validation unavailable fails closed and persists rejected attempt with `ValidationUnavailable`.
- [ ] No rejected scan creates attendance record.
- [ ] No rejected scan emits `TicketCheckedIn`.

## Tests

- [ ] Integration test: malformed ticket rejected and scan attempt persisted.
- [ ] Integration test: unknown ticket rejected and scan attempt persisted.
- [ ] Integration test: invalid ticket rejected and scan attempt persisted.
- [ ] Integration test: canceled ticket rejected and scan attempt persisted.
- [ ] Resilience test: validation timeout creates rejected attempt with `ValidationUnavailable`.
- [ ] Resilience test: validation unavailable creates rejected attempt with `ValidationUnavailable`.
- [ ] Integration test: no rejected scan creates attendance record.
- [ ] Integration test: no rejected scan writes `TicketCheckedIn` outbox message.
