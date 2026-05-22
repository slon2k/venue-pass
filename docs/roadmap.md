# VenuePass — Roadmap

## Now

- **Milestone**: 01 - Foundation
  - Outcome: The modular monolith foundation is in place with enforced boundaries, baseline CI, and the first Events vertical slice (CreateVenue) implemented end-to-end.
  - Features:
    - Solution scaffolding finalized
    - Module boundaries defined and enforced with architecture tests
    - Baseline CI workflow (`ci.yml`) running restore, build, and solution-level tests
    - Events module first vertical slice (domain, persistence, migrations, endpoint, tests)
    - Core architecture and planning docs aligned with implemented behavior

## Next

- **Milestone**: 02 - Messaging and Ticketing Bootstrap
  - Outcome: Reliable Outbox-backed integration flow is proven between Events and Ticketing for initial publish/inventory scenarios.

## Later

- 03 - Ticketing Sales Flows (offers, reservations, orders)
- 04 - Attendance Check-In Flows
- 05 - Identity Module First Cut (custom login, roles, JWT issuance)
- 06 - Integration CI Expansion and Migration Smoke Checks

## Milestone Outcomes

| Milestone | Outcome | Status |
|-----------|---------|--------|
| 01 - Foundation | Foundation baseline complete with first Events slice and CI | In progress |
| 02 - Messaging and Ticketing Bootstrap | Outbox-backed cross-module flow validated | Not started |
| 03 - Ticketing Sales Flows | Ticketing core commercial lifecycle implemented | Not started |
| 04 - Attendance Check-In Flows | Admission/check-in lifecycle implemented | Not started |
| 05 - Identity Module First Cut | Internal identity and token issuance baseline implemented | Not started |
| 06 - Integration CI Expansion and Migration Smoke Checks | CI verifies integration and migration paths | Not started |

## Risks and Dependencies

- Outbox reliability and idempotency patterns must be validated early to avoid rework.
- Boundary tests must evolve with module growth to prevent accidental coupling.
- Eventual consistency behavior requires explicit acceptance criteria in cross-module slices.
- Time-boxing as a solo project can constrain parallel progress across modules.
