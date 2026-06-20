# Capability E: Attendance status retrieval

## Summary

Provide Attendance read API to retrieve check-in state by ticket code or ticket ID for operational use cases.

Retrieval reports Attendance state only. It does not call Ticketing validation and does not determine current ticket validity.

## Scope

- In scope:
  - Deliver `GetAttendanceStatus` endpoint and handler
  - Support lookup by ticket code
  - Support lookup by ticket ID
  - Return attendance/check-in status for known tickets
  - Return check-in metadata when a successful attendance record exists
  - Return projection metadata when ticket is known but not checked in
  - Return not found when no attendance record or projection exists
- Out of scope:
  - Ticket validity re-check in retrieval path
  - Projection-only admission decisions
  - Analytics or aggregated dashboards
  - Customer-facing self-service endpoints
  - Historical scan-attempt search/reporting

## Functional Requirements

- [ ] AR-04 Retrieval returns check-in status for known ticket.
- [ ] AR-05 Retrieval reports Attendance state only and does not perform Ticketing validity checks.
- [ ] E2 Lookup supports ticket code and ticket ID.
- [ ] E3 Response includes required fields:
  - ticket ID
  - ticket code
  - canonical event reference
  - check-in status
  - checked-in timestamp when available
  - attendance record ID when available
  - projected ticket status when available
  - last rejection reason or scan metadata when included
- [ ] E4 Unknown ticket in Attendance context returns not found.
- [ ] E5 Known projected ticket without attendance record returns `NotCheckedIn`, not `404`.
- [ ] E6 Ticket code lookup normalizes input consistently with Attendance/Ticketing ticket-code rules.
- [ ] E7 Retrieval endpoint does not create scan attempts.
- [ ] E8 Retrieval endpoint does not mutate Attendance or Ticketing state.
- [ ] E9 Scan attempts alone do not make a ticket known unless an attendance record or ticket projection exists.

## Endpoint Contract

### Preferred route

```http
GET /attendance/status?ticketCode={ticketCode}
GET /attendance/status?ticketId={ticketId}
```

- Auth: operational role
- Lookup inputs:
  - `ticketCode`
  - or `ticketId`
- Exactly one lookup key should be supplied.
- Ticket code input is normalized before lookup.
- Ticket ID input must be a valid ID format.

### Alternative route shape

If route parameters are preferred, use separate unambiguous routes:

```http
GET /attendance/status/by-code/{ticketCode}
GET /attendance/status/by-ticket/{ticketId}
```

Avoid ambiguous route shape:

```http
GET /attendance/records/{ticketIdOrCode}
```

unless parsing/precedence rules are explicitly defined.

## Response Contract

Successful response should include Attendance-known state.

Minimum fields:

- ticket ID
- ticket code
- canonical event reference
- check-in status
- attendance record ID, if checked in
- checked-in timestamp, if checked in
- projected ticket status, if projection exists
- last rejection reason or scan metadata, if included

Suggested check-in statuses:

```text
NotCheckedIn
CheckedIn
```

Optional projected ticket statuses may include:

```text
Issued
Canceled
Unknown
```

Important distinction:

- `checkInStatus` is Attendance-owned state.
- `projectedTicketStatus` is a local read model from Ticketing events.
- Retrieval does not confirm current Ticketing validity.

## Not Found Behavior

Return not found when:

- lookup by ticket code finds no ticket projection and no attendance record;
- lookup by ticket ID finds no ticket projection and no attendance record.

Rejected scan attempts alone should not cause the ticket to be treated as known.

## Acceptance Criteria

- [ ] Retrieval by ticket code returns attendance state when known.
- [ ] Retrieval by ticket ID returns attendance state when known.
- [ ] Known projected ticket with no attendance record returns `NotCheckedIn`.
- [ ] Checked-in ticket returns `CheckedIn` with attendance record ID and checked-in timestamp.
- [ ] Unknown lookup key returns not found.
- [ ] Endpoint does not call Ticketing validation contract.
- [ ] Endpoint does not create scan attempts.
- [ ] Endpoint does not mutate Attendance state.
- [ ] Ticket code lookup uses normalized ticket code.
- [ ] Ambiguous or invalid lookup input is handled deterministically.

## Test Checklist

- [ ] Integration test: lookup by ticket code returns known projected ticket.
- [ ] Integration test: lookup by ticket ID returns known projected ticket.
- [ ] Integration test: projected ticket without attendance record returns `NotCheckedIn`.
- [ ] Integration test: checked-in ticket returns `CheckedIn` with attendance metadata.
- [ ] Integration test: canceled projected ticket can be retrieved without Ticketing validation.
- [ ] Integration test: unknown ticket returns not found.
- [ ] Integration test: rejected scan attempt alone does not make unknown ticket retrievable as known.
- [ ] Integration test: ticket code lookup normalizes input.
- [ ] Authorization test: retrieval endpoint requires operational role.
- [ ] Unit/integration test: retrieval path does not invoke Ticketing validation.
- [ ] Unit/integration test: retrieval path does not create scan attempts.

## Dependencies

- Capability B data model and indexes.
- Capability C projection upsert path for pre-check-in visibility.
- Capability D scan/check-in flow for end-to-end checked-in state tests.

## Risks

- Ambiguous lookup precedence can cause inconsistent reads if code and ID paths diverge.
- Incomplete projection data can reduce operator usefulness before first scan.
- Returning projected ticket status may be mistaken for current Ticketing validity unless response naming is clear.
- Treating rejected scan attempts as known tickets could create confusing operator results.
