# VenuePass — Roadmap

## Now

- **Milestone**: M1 Foundation and First Events Slices
  - Outcome: The modular monolith foundation is in place with enforced boundaries, baseline CI, and first vertical slices in Events implemented and tested.
  - Features:
    - Solution scaffolding finalized
    - Module boundaries defined and enforced with architecture tests
    - Baseline CI pipeline running restore, build, architecture tests, and unit tests
    - Events module first vertical slices (domain, persistence, endpoints, tests)
    - Core architecture and planning docs aligned with implemented behavior

## Next

- **Milestone**: M2 Cross-Module Messaging and Ticketing Bootstrap
  - Outcome: Reliable Outbox-backed integration flow is proven between Events and Ticketing for initial publish/inventory scenarios.

## Later

- M3 Ticketing Sales Flows (offers, reservations, orders)
- M4 Attendance Check-In Flows
- M5 Identity Module First Cut (custom login, roles, JWT issuance)
- M6 Integration CI Expansion and Migration Smoke Checks

## Milestone Outcomes

| Milestone | Outcome | Status |
|-----------|---------|--------|
| M1 Foundation and First Events Slices | Foundation complete with first Events slices and CI baseline | In progress |
| M2 Cross-Module Messaging and Ticketing Bootstrap | Outbox-backed cross-module flow validated | Not started |
| M3 Ticketing Sales Flows | Ticketing core commercial lifecycle implemented | Not started |
| M4 Attendance Check-In Flows | Admission/check-in lifecycle implemented | Not started |
| M5 Identity Module First Cut | Internal identity and token issuance baseline implemented | Not started |
| M6 Integration CI Expansion and Migration Smoke Checks | CI verifies integration and migration paths | Not started |

## Risks and Dependencies

- Outbox reliability and idempotency patterns must be validated early to avoid rework.
- Boundary tests must evolve with module growth to prevent accidental coupling.
- Eventual consistency behavior requires explicit acceptance criteria in cross-module slices.
- Time-boxing as a solo project can constrain parallel progress across modules.
