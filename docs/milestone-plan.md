# VenuePass — Milestone Plan (01 - Events: Venues & Manifest Templates)

## Milestone Outcome

The Events module supports venue management and manifest template management end-to-end, with persistence in the `events` schema and retrieval APIs for both capabilities.

## In Scope

- [ ] Venue management
- [ ] Manifest template management
- [ ] Events persistence and outbox readiness

## Out of Scope

- Event creation and publication
- Manifest snapshot creation from templates
- Ticketing synchronization from published events
- Attendance workflows
- Identity module implementation beyond existing development setup
- Venue update/delete flows
- Manifest template update/delete flows

## Capability Breakdown

### Venue management

- [ ] Create venue
- [ ] Get venue
- [ ] Harden venue validation and duplicate rules

### Manifest template management

- [x] Create manifest template
- [x] Get manifest template

### Events persistence and outbox readiness

- [ ] Persist venue and manifest template aggregate structure in the `events` schema
- [ ] Add migrations required for venues and manifest templates
- [ ] Add outbox table in the `events` schema without publication flow yet

## Initial Manifest Template Scope

Seated structure plus general admission areas.

## Definition of Done

- [ ] All in-scope capability issues implemented and merged
- [ ] Venue creation and retrieval work through the API
- [ ] Manifest template creation and retrieval work through the API
- [ ] Venue and manifest template data persist in the `events` schema
- [ ] Venue duplicate rule is enforced consistently
- [ ] Architecture tests still pass
- [ ] Docs updated if contracts or behavior change

## Validation Checklist

- [ ] `dotnet build VenuePass.slnx --configuration Release /warnaserror`
- [ ] `dotnet test VenuePass.slnx --configuration Release`
- [ ] Venue API flow tested end-to-end
- [ ] Manifest template API flow tested end-to-end
- [ ] Database schema contains required `events` objects for venues, templates, and outbox readiness

## Risks and Dependencies

- Manifest template modeling can expand quickly if seating structure rules are not kept tight.
- Template persistence may drive several EF Core mapping decisions at once.
- Outbox readiness should not accidentally pull publication behavior from M02 into this milestone.
- Retrieval shape decisions made here will influence later event-creation flows.
