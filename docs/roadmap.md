# VenuePass — Roadmap

## Now

- **Milestone**: 00 - Foundation: Project Scaffolding
  - Outcome: The modular monolith foundation is in place with enforced boundaries, baseline CI, and the first Events vertical slice (CreateVenue) implemented end-to-end.
  - Features:
    - Solution scaffolding finalized
    - Module boundaries defined and enforced with architecture tests
    - Baseline CI workflow (`ci.yml`) running restore, build, and solution-level tests
    - Events module first vertical slice (domain, persistence, migrations, endpoint, tests)
    - Core architecture and planning docs aligned with implemented behavior

## Next

- **Milestone**: 01 - Events: Venues & Manifest Templates
  - Outcome: Venues and manifest templates are modeled, persisted, and exposed through the Events module.

## Later

- 02 - Events: Event Creation & Publication
- 03 - Ticketing: Event Sync, Inventory & Offers
- 04 - Ticketing: Reservation, Orders & Ticket Issuance
- 05 - Attendance: Check-In
- 06 - Identity: Users, Roles & Authentication
- 07 - Integration & Operational Hardening

## Milestone Outcomes

| Milestone | Outcome | Status |
|-----------|---------|--------|
| 00 - Foundation: Project Scaffolding | Foundation baseline complete with first Events slice and CI | In progress |
| 01 - Events: Venues & Manifest Templates | Venues and manifest templates are modeled and retrievable | Not started |
| 02 - Events: Event Creation & Publication | Event lifecycle and publication flow are operational | Not started |
| 03 - Ticketing: Event Sync, Inventory & Offers | Ticketing synchronizes published events and exposes sellable inventory | Not started |
| 04 - Ticketing: Reservation, Orders & Ticket Issuance | Reservation and purchase lifecycle produces tickets | Not started |
| 05 - Attendance: Check-In | Admission and check-in lifecycle is implemented | Not started |
| 06 - Identity: Users, Roles & Authentication | Internal identity and authentication baseline is implemented | Not started |
| 07 - Integration & Operational Hardening | Integration reliability, CI expansion, and operational checks are in place | Not started |

## Risks and Dependencies

- Outbox reliability and idempotency patterns must be validated early to avoid rework.
- Boundary tests must evolve with module growth to prevent accidental coupling.
- Eventual consistency behavior requires explicit acceptance criteria in cross-module slices.
- Time-boxing as a solo project can constrain parallel progress across modules.
