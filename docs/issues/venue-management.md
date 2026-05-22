# Venue management

## Summary

Enable the Events module to manage venues end-to-end, starting with venue creation and retrieval.

## Scope

- In scope:
  - Create venue
  - Get venue
  - Harden venue validation and duplicate rules
- Out of scope:
  - Venue update/delete flows
  - Manifest template behavior
  - Event creation and publication

## Acceptance Criteria

- [ ] A venue can be created through the Events API.
- [ ] A venue can be retrieved through the Events API.
- [ ] Venue name and address validation is enforced consistently.
- [ ] Duplicate venue rules are enforced consistently.

## Vertical Slices

- [ ] Create venue
- [ ] Get venue
- [ ] Harden venue validation and duplicate rules

## Risks and Assumptions

- Retrieval shape may need a small response contract decision.
- Duplicate detection should remain aligned with the eventual Events domain rules.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
