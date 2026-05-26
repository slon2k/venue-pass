# Manifest template management

## Summary

Enable the Events module to manage manifest templates end-to-end, starting with create and get flows for seated and general-admission structures.

## Scope

- In scope:
  - Create manifest template
  - Get manifest template
  - Persist template structure in the Events schema
- Out of scope:
  - Event creation and publication
  - Manifest snapshot creation from templates
  - Ticketing synchronization from published events

## Acceptance Criteria

- [ ] A manifest template can be created through the Events API.
- [ ] A manifest template can be retrieved through the Events API.
- [ ] Seated structure and general admission areas are persisted correctly.
- [ ] Template validation rejects invalid or incomplete structures.

## Vertical Slices

- [x] Create manifest template
- [x] Get manifest template

## Risks and Assumptions

- Template shape may need minor adjustments once event publication work begins.
- Persistence mapping may require additional owned-type or collection configuration.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
