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

- [x] A venue can be created through the Events API.
- [x] A venue can be retrieved through the Events API.
- [x] Venue name and address validation is enforced consistently.
- [x] Duplicate venue rules are enforced consistently.

## Vertical Slices

- [x] Create venue
- [x] Get venue
- [x] Harden venue validation and duplicate rules

## Risks and Assumptions

- Retrieval shape may need a small response contract decision.
- Duplicate detection should remain aligned with the eventual Events domain rules.

## Definition of Done

- [x] Acceptance criteria met
- [x] Tests passing
- [x] Docs updated if behavior changed
