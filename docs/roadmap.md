# VenuePass — Roadmap

## Now

- **Milestone**: 04 - Ticketing: Reservation, Orders & Ticket Issuance
  - Outcome: Reservation and purchase lifecycle produces tickets.

## Next

- **Milestone**: 05 - Attendance: Check-In
  - Outcome: Admission and check-in lifecycle is implemented.

## Later

- 06 - Identity: Users, Roles & Authentication
- 07 - Integration & Operational Hardening

## Milestone Outcomes

| Milestone | Outcome | Status |
| --------- | ------- | ------ |
| 00 - Foundation: Project Scaffolding | Foundation baseline complete with first Events slice and CI | Closed |
| 01 - Events: Venues & Manifest Templates | Venues and manifest templates are modeled and retrievable | Closed |
| 02 - Events: Event Creation & Publication | Event lifecycle and publication flow are operational | Closed |
| 03 - Ticketing: Event Sync, Inventory & Offers | Ticketing synchronizes published events and exposes sellable inventory | Closed |
| 04 - Ticketing: Reservation, Orders & Ticket Issuance | Reservation and purchase lifecycle produces tickets | In progress |
| 05 - Attendance: Check-In | Admission and check-in lifecycle is implemented | Not started |
| 06 - Identity: Users, Roles & Authentication | Internal identity and authentication baseline is implemented | Not started |
| 07 - Integration & Operational Hardening | Integration reliability, CI expansion, and operational checks are in place | Not started |

## Milestone Files

- Milestone detail files are stored in `docs/milestones/`.
- Use those files as the source of truth for milestone scope and checklists.
- Use this roadmap to capture status transitions and sequencing changes.

## Risks and Dependencies

- Outbox reliability and idempotency patterns must be validated early to avoid rework.
- Boundary tests must evolve with module growth to prevent accidental coupling.
- Eventual consistency behavior requires explicit acceptance criteria in cross-module slices.
- Time-boxing as a solo project can constrain parallel progress across modules.

## Deferred Decisions

- Pricing model remains intentionally simple in early stages (Offer + PriceZones with one price per zone).
- Advanced two-axis pricing (Offer + Target + PriceType matrix) is deferred and will be reconsidered only after cross-module reliability and missing core flows are complete.
- Re-evaluation target: Milestone 05+ planning, based on demonstrated product need.
