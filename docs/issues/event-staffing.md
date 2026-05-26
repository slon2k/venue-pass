# Event staffing

## Summary

Enable staffing for events by supporting manager assignment with role-based authorization and consistent failure behavior.

## Scope

- In scope:
  - Assign event manager domain behavior
  - Assign event manager application/endpoint flow
  - EventManager role claim enforcement for assignment
  - Integration tests for success and failure paths
- Out of scope:
  - Event publication
  - Manifest snapshot creation
  - Identity provider expansion beyond required claim checks

## Acceptance Criteria

- [ ] An event manager can be assigned to an existing event via API.
- [ ] Assignment endpoint requires EventManager role claim.
- [ ] Unauthorized requests are rejected consistently.
- [ ] Invalid or not-found assignment requests are rejected consistently.
- [ ] Assignment behavior is covered by integration tests.

## Vertical Slices

- [ ] B1: Implement AssignEventManager domain and application behavior
- [ ] B2: Deliver AssignEventManager endpoint with EventManager claim requirement
- [ ] B3: Add integration tests for success, unauthorized, and invalid/not-found paths

## Risks and Assumptions

- Claim naming/shape must remain aligned with local JWT setup and future Identity module decisions.
- Assignment policy may evolve if role model expands in Milestone 06.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
