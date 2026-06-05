# Event staffing

## Summary

Introduce JWT-based authorization across all Events module endpoints, auto-assign the creating EventManager to new events, and provide an Admin-only reassignment path for exceptional cases.

## Roles

| Role | Responsibilities |
|------|-----------------|
| Admin | Platform ops: venues, manifest templates, user management, manager reassignment |
| EventManager | Event lifecycle: create, modify, publish their own events |

## Endpoint Policy Table

| Endpoint | Policy |
|----------|--------|
| `POST /venues` | Admin |
| `GET /venues/{id}` | Any authenticated |
| `POST /manifest-templates` | Admin |
| `GET /manifest-templates/{id}` | Any authenticated |
| `POST /events` | EventManager |
| `POST /events/{id}/reassign-manager` | Admin |
| `POST /events/{id}/publish` | EventManager + domain check (assigned) |
| `GET /events/{id}` | Any authenticated |

Unauthenticated requests to all endpoints return 401. Role-unauthorized requests return 403.

## Scope

- In scope:
  - `UserId` value object in `BuildingBlocks.Domain`
  - JWT Bearer auth wired in API (dev signing key)
  - `TestAuthHandler` for integration tests
  - Auth guards applied to all 8 endpoints per policy table (includes M01 endpoint retrofit)
  - `AssignedManagerId` on `Event` aggregate, auto-assigned from JWT `sub` on creation
  - `ReassignEventManager` domain behavior and endpoint (Admin only)
  - `GetEvent` response includes `AssignedManagerId`
  - Integration tests for auth enforcement and reassignment
- Out of scope:
  - Publication guard (Capability C)
  - Ex-manager self-reassignment (TBD, deferred)
  - Real identity provider / Entra auth (Milestone 06)

## Acceptance Criteria

- [x] Unauthenticated requests to all endpoints return 401
- [x] Role-unauthorized requests return 403
- [x] `POST /events` requires EventManager role; `AssignedManagerId` is set to caller's `sub`
- [x] Admin can reassign the manager of any event; `AssignedManagerId` updated in persistence
- [x] Unknown event on reassign returns 404
- [x] `GET /events/{id}` response includes `AssignedManagerId`
- [x] All existing A-slice integration tests pass with auth enabled

## Vertical Slices

- [x] B1: Auth infrastructure, endpoint guards, and CreateEvent auto-assignment
- [x] B2: ReassignEventManager domain and endpoint
- [x] B3: Integration tests for staffing and auth enforcement

## Risks and Assumptions

- Claim naming/shape must remain aligned with local JWT setup and future Identity module decisions.
- `newManagerId` on reassignment is not validated against a real user store until Milestone 06.
- Ex-manager reassignment ability is explicitly deferred and out of scope for M02.

## Definition of Done

- [x] Acceptance criteria met
- [x] Tests passing
- [x] Docs updated if behavior changed
