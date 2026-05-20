# VenuePass — Milestone Plan (M1 Foundation and First Events Slices)

## Milestone Outcome

VenuePass has a stable modular-monolith foundation with enforced boundaries, baseline CI, and initial Events vertical slices delivered end-to-end.

## In Scope

- [ ] Finalize solution scaffolding and module project layout
- [ ] Enforce module boundaries via architecture tests
- [ ] Establish baseline CI pipeline (restore, build, architecture tests, unit tests)
- [ ] Deliver first Events vertical slices with domain, persistence, endpoint mapping, and tests
- [ ] Align core docs with implemented architecture and delivery workflow

## Out of Scope

- Full cross-module integration event choreography across all modules
- Ticketing sales lifecycle completion (offers/reservations/orders)
- Attendance admission lifecycle completion
- Full Identity module implementation beyond early JWT dev setup
- Production-grade deployment or cloud runtime optimization

## Definition of Done

- [ ] All in-scope features implemented and merged
- [ ] Architecture tests pass and protect intended boundaries
- [ ] Unit tests for implemented slices pass in CI
- [ ] Build passes cleanly in CI baseline pipeline
- [ ] Architecture, roadmap, and milestone docs are updated to match implementation

## Validation Checklist

- [ ] `dotnet restore` passes
- [ ] `dotnet build` passes
- [ ] Architecture test suite passes
- [ ] Events module unit tests pass
- [ ] First Events API slice works in local run against SQL Server container

## Risks and Dependencies

- Architecture test rules may need iterative tuning as module internals become concrete.
- Outbox-related decisions can affect slice design even if full integration is deferred.
- CI flakiness risk exists while initial test/database setup stabilizes.
- Solo execution means sequencing discipline is required to avoid scope creep.
