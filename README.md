# VenuePass

VenuePass is a .NET modular monolith for event management with ticketing and attendance.

It is being built as a **demo project** to practice:

- modular monolith architecture
- strong module boundaries
- domain modeling
- reliable cross-module communication via Outbox
- eventual consistency
- pragmatic internal structure in .NET

## Status

🚧 **Work in progress** — early architecture and scaffolding phase.

Current focus:

- solution scaffolding
- module boundaries
- architecture tests
- CI baseline
- first `Events` slices

---

## High-Level Architecture

VenuePass is structured as a **modular monolith** with four main modules:

- **Events** — canonical event structure
- **Ticketing** — commercial inventory and sales
- **Attendance** — admission/check-in decisions
- **Identity** — users and access

### Principles

- each module owns its own data
- modules do not access each other’s tables
- modules do not share business entities
- cross-module state changes are announced via integration events
- consumers may fetch additional source-of-truth data from the owning module when needed
- immediate consistency inside a module
- eventual consistency across modules

### Communication model

- **inside a module**: direct calls, handlers, domain logic
- **across modules**:
  - thin integration events via Outbox
  - synchronous contract calls for read/validation when needed

Example synchronization pattern:

1. `Events` publishes `EventPublished(EventId, ManifestId)`
2. `Ticketing` receives the event
3. `Ticketing` fetches manifest data from `Events`
4. `Ticketing` creates its own local `Inventory`

---

## Planned Modules

| Module | Responsibility |
|---|---|
| Events | Venue setup, manifest templates, manifests, event lifecycle |
| Ticketing | Inventory, offers, reservations, orders, tickets |
| Attendance | Ticket validation, scan attempts, check-ins |
| Identity | Registration, login, JWT issuance, roles |

---

## Tech Stack

- **.NET** — latest LTS at project start
- **ASP.NET Core Minimal APIs**
- **EF Core**
- **SQL Server** in Docker
- **Schema-per-module** persistence
- **Hand-rolled Outbox** for integration events
- **xUnit** for tests
- **Architecture tests** for boundary enforcement

---

## Process Docs

- [Docs planning and execution flow](docs/github-projects-process.md)
