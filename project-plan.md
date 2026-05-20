# Project Scope: VenuePass

## Problem Statement

Event organizers and venues need a reliable, modular system to manage events, ticketing, and attendance with strong domain boundaries and robust internal architecture. Existing solutions often lack clear module separation, making maintenance and evolution difficult.

## Goals

- [ ] Demonstrate modular monolith architecture in .NET
- [ ] Enforce strong module boundaries (Events, Ticketing, Attendance, Identity)
- [ ] Implement reliable cross-module communication (Outbox pattern)
- [ ] Achieve eventual consistency between modules
- [ ] Provide a reference for pragmatic .NET internal structure

## Non-Goals

- Not building a production-ready SaaS or commercial product
- No support for multi-tenant or external integrations
- No focus on UI/UX beyond minimal API endpoints

## Constraints

- Solo developer
- Time-boxed to learning and demonstration (not commercial timelines)
- .NET technology stack only
- Use xUnit for tests

## First Release Scope

- Solution scaffolding and module boundaries
- Architecture and boundary tests
- Minimal API endpoints for Events module
- CI pipeline with build, test, and architecture checks

## Success Criteria

- All modules have clear, enforced boundaries
- Architecture and unit tests pass in CI
- Outbox pattern demonstrated for cross-module messaging
- Documentation (architecture, delivery plan, tech decisions) is up to date

## Open Questions

- [ ] What is the best approach for evolving module contracts over time?
- [ ] How to balance demo simplicity with realistic domain complexity?
