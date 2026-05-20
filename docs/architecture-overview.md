# VenuePass — Architecture Overview

## 1. System Purpose

VenuePass is a .NET modular monolith for event management with ticketing and attendance.

Canonical terms and definitions are maintained in `docs/ubiquitous-language.md`.

It exists to practice:

- module boundary enforcement
- domain ownership and modeling
- cross-module communication via reliable integration events
- eventual consistency across modules
- clean internal module structure

---

## 2. Module Map

```text
┌─────────────────────────────────────────────────────────┐
│                    VenuePass.Api                        │
│              (host, routing, middleware)                │
└────────┬───────────┬──────────────┬──────────────┬──────┘
         │           │              │              │
    ┌────▼───┐ ┌─────▼─────┐  ┌─────▼──────┐  ┌────▼─────┐
    │ Events │ │ Ticketing │  │ Attendance │  │ Identity │
    └────────┘ └───────────┘  └────────────┘  └──────────┘
         │           │              │              │
    ┌────▼───────────▼──────────────▼──────────────▼──────┐
    │              VenuePass.BuildingBlocks               │
    │     (primitives, result types, outbox contracts,    │
    │       messaging and current-user abstractions)      │
    └─────────────────────────────────────────────────────┘
```

| Module | Source of truth for | Key concepts |
|--------|---------------------|--------------|
| Events | Canonical event structure | Event, Venue, ManifestTemplate, Manifest, Seat, GeneralAdmissionArea |
| Ticketing | Commercial inventory and sales | Offer, PriceLevel, Inventory, InventorySeat, GeneralAdmissionPool, Reservation, Order, Ticket |
| Attendance | Admission decisions | CheckIn, ScanAttempt, AttendanceRecord |
| Identity | Users and access | User, Role, Permission, JWT issuance |

---

## 3. Tech Stack Summary

| Concern | Choice |
|---------|--------|
| Runtime |  Latest LTS version of .NET at project start |
| API | Minimal APIs with per-feature endpoint mapping |
| Dispatch | Direct injection — co-located Command + Handler per feature |
| Persistence | EF Core, one DbContext per module, one DB with schema-per-module |
| Database | SQL Server in Docker |
| Cross-module events | Hand-rolled Outbox (EF Core + background service) |
| Identity (early slices) | Stub JWT via `dotnet user-jwts` |
| Identity (Identity slice) | Custom login endpoint, hand-rolled user table, JWT issuance |
| Testing | xUnit, architecture tests for boundary enforcement |
| Containerization | Docker Compose for SQL Server (app runs on host) |

---

## 4. Project Structure

```text
VenuePass.slnx
│
├─ src/
│  ├─ VenuePass.Api/
│  │  ├─ Program.cs
│  │  ├─ DependencyInjection/
│  │  └─ Extensions/
│  │
│  ├─ VenuePass.BuildingBlocks/
│  │  ├─ Domain/          (small base abstractions only)
│  │  ├─ Application/     (result types, shared interfaces)
│  │  ├─ Infrastructure/  (current-user, outbox abstractions, helpers)
│  │  └─ Messaging/       (integration event base/contracts)
│  │
│  └─ Modules/
│     ├─ VenuePass.Modules.Events/
│     │  ├─ Features/        (one folder per use case)
│     │  ├─ Domain/          (organized by aggregate / business concept)
│     │  ├─ Infrastructure/  (DbContext, configurations, outbox)
│     │  ├─ Contracts/       (interfaces exposed to other modules)
│     │  └─ ModuleConfiguration.cs
│     │
│     ├─ VenuePass.Modules.Ticketing/
│     │  ├─ Features/
│     │  ├─ Domain/
│     │  ├─ Infrastructure/
│     │  ├─ Contracts/
│     │  └─ ModuleConfiguration.cs
│     │
│     ├─ VenuePass.Modules.Attendance/
│     │  ├─ Features/
│     │  ├─ Domain/
│     │  ├─ Infrastructure/
│     │  ├─ Contracts/
│     │  └─ ModuleConfiguration.cs
│     │
│     └─ VenuePass.Modules.Identity/
│        ├─ Features/
│        ├─ Infrastructure/
│        ├─ Contracts/
│        └─ ModuleConfiguration.cs
│
├─ tests/
│  ├─ VenuePass.ArchitectureTests/
│  ├─ VenuePass.Modules.Events.Tests/
│  ├─ VenuePass.Modules.Ticketing.Tests/
│  ├─ VenuePass.Modules.Attendance.Tests/
│  └─ VenuePass.Modules.Identity.Tests/
│
├─ docs/
│
└─ docker-compose.yml
```

---

## 5. Internal Module Structure

Each module is **one project**.

The internal organization is:

- **feature-first** in `Features/`
- **aggregate/business-concept-first** in `Domain/`
- **technical concerns** in `Infrastructure/`
- **public cross-module interfaces** in `Contracts/`

### Recommended structure pattern

> **Organize the domain by aggregate / business concept, not by technical artifact type.**

The aggregate-first structure keeps related domain code together:

- aggregate root
- child entities
- local value objects
- domain events
- policies/rules

This makes the model easier to navigate, keeps aggregate boundaries visible, and reduces scattering of related logic across technical folders

Prefer this:

```text
Domain/
  Venues/
  ManifestTemplates/
  Manifests/
  Events/
```

over this:

```text
Domain/
  Entities/
  ValueObjects/
  Events/
  Rules/
```

### Recommended module shape

```text
VenuePass.Modules.Events/
├─ Features/
│  ├─ CreateVenue/
│  │  ├─ CreateVenue.cs
│  │  ├─ CreateVenueEndpoint.cs
│  │  └─ CreateVenueValidator.cs
│  ├─ CreateManifestTemplate/
│  ├─ CreateEvent/
│  ├─ PublishEvent/
│  └─ ...
├─ Domain/
│  ├─ Venues/
│  ├─ ManifestTemplates/
│  ├─ Manifests/
│  └─ Events/
├─ Infrastructure/
│  ├─ EventsDbContext.cs
│  ├─ Configurations/
│  ├─ Outbox/
│  └─ Migrations/
├─ Contracts/
│  └─ IEventsModule.cs
└─ ModuleConfiguration.cs
```

### Domain structure examples

```text

VenuePass.Modules.Events/
├─ Domain/
│  ├─ Venues/
│  │  └─ Venue.cs
│  │
│  ├─ ManifestTemplates/
│  │  ├─ ManifestTemplate.cs
│  │  ├─ Section.cs
│  │  ├─ Row.cs
│  │  ├─ Seat.cs
│  │  └─ GeneralAdmissionArea.cs
│  │
│  ├─ Manifests/
│  │  ├─ Manifest.cs
│  │  ├─ ManifestSection.cs
│  │  ├─ ManifestRow.cs
│  │  ├─ ManifestSeat.cs
│  │  ├─ ManifestGeneralAdmissionArea.cs
│  │  └─ ManifestLockedDomainEvent.cs
│  │
│  └─ Events/
│     ├─ Event.cs
│     ├─ EventManagerAssignment.cs
│     └─ EventPublishedDomainEvent.cs
...

VenuePass.Modules.Ticketing/
├─ Domain/
│  ├─ PublishedEvents/
│  │  └─ PublishedEventReference.cs
│  │
│  ├─ Inventories/
│  │  ├─ Inventory.cs
│  │  ├─ InventorySeat.cs
│  │  └─ GeneralAdmissionPool.cs
│  │
│  ├─ Offers/
│  │  ├─ Offer.cs
│  │  └─ PriceLevel.cs
│  │
│  ├─ Reservations/
│  │  ├─ Reservation.cs
│  │  ├─ ReservedSeatItem.cs
│  │  └─ ReservedGeneralAdmissionItem.cs
│  │
│  ├─ Orders/
│  │  └─ Order.cs
│  │
│  └─ Tickets/
│     ├─ Ticket.cs
│     ├─ TicketType.cs
│     ├─ TicketIssuedDomainEvent.cs
│     └─ TicketCanceledDomainEvent.cs
...

VenuePass.Modules.Attendance/
├─ Domain/
│  ├─ CheckIns/
│  │  ├─ CheckIn.cs
│  │  └─ TicketCheckedInDomainEvent.cs
│  │
│  ├─ ScanAttempts/
│  │  └─ ScanAttempt.cs
│  │
│  └─ AttendanceRecords/
│     └─ AttendanceRecord.cs
...
```

### Dependency direction

```text
Features  → Domain
Features  → Infrastructure
Infrastructure → Domain
Domain   -X→ Infrastructure
Domain   -X→ Features
```

---

## 6. Module Boundary Rules

1. Modules do **not** access each other's database tables
2. Modules do **not** share business entities
3. Cross-module entity relationships use IDs (`Guid`)
4. Cross-module data transfer uses contracts and integration-event payloads, not shared business entities
5. State changes across modules are announced via **integration events** (Outbox-backed)
6. Synchronous cross-module calls are allowed **only for read/validation** when fresh data is required
7. Modules may expose small `Contracts/` interfaces for synchronous queries
8. Consumers must not depend on another module’s internals

---

## 7. Communication Model

### Inside a module

- Direct method calls
- Domain events (in-process, same transaction)
- Immediate consistency

### Across modules

| Style | When | Delivery |
|-------|------|----------|
| Synchronous query | Caller needs fresh source-of-truth data for a decision | Direct call via module contract interface |
| Integration event | State changed, other modules should react | Outbox → background dispatcher → subscriber handler |

### Outbox flow

1. Handler executes command
2. Module updates its own state
3. Module writes integration event to its Outbox table (same transaction)
4. Transaction commits
5. Background dispatcher picks up pending events
6. Subscriber module handles event asynchronously
7. Handlers are idempotent (at-least-once delivery)

### Synchronization pattern

Recommended cross-module synchronization follows a **notify-and-fetch** model:

1. The owning module publishes a thin integration event with identifiers only
2. A consuming module receives the event
3. If it needs more data, it fetches source-of-truth data through the owning module’s contract
4. The consumer stores its own local model or projection

Example:

- `Events` publishes `EventPublished(EventId, ManifestId)`
- `Ticketing` receives the event
- `Ticketing` requests manifest data from `Events`
- `Ticketing` creates its own `Inventory`

### Consistency model

- Inside a module: **immediate consistency**
- Across modules: **eventual consistency**

---

## 8. Persistence Model

| Property | Value |
|----------|-------|
| Database | Single SQL Server instance |
| Schema separation | One schema per module (`events`, `ticketing`, `attendance`, `identity`) |
| DbContext | One per module, scoped to its own schema |
| Migrations | Per-module, separate migration history |
| Shared tables | None — not even for Outbox (each module has its own) |

---

## 9. Identity and Authorization

### Graduated approach

| Phase | Implementation |
|-------|----------------|
| Early slices (Events, Ticketing, Attendance) | `dotnet user-jwts` generates dev tokens with role claims |
| Identity slice | Custom login endpoint, hand-rolled user/role tables in `identity` schema, JWT issuance |
| Future (optional) | Swap to Keycloak or Entra ID — modules don't change |

### Authorization enforcement

- Modules validate JWT claims via standard ASP.NET authorization policies and middleware
- Event-scoped ownership (e.g., "this EventManager owns this event") is enforced inside the owning module
- Identity module does **not** know about business assignments

---

## 10. Key Architectural Constraints

1. No distributed transactions across modules — even though one physical DB
2. No shared EF Core model across the application
3. No direct use of another module’s internals; cross-module interaction uses contracts and integration events
4. Domain layer has zero infrastructure dependencies
5. Integration events announce state changes across module boundaries
6. Consumers may synchronously fetch source-of-truth data from the owning module when needed for synchronization or validation
7. Architecture tests enforce these rules automatically

## 11. Practical Summary

### VenuePass is a modular monolith with

- strong business-aligned module boundaries
- one project per module
- feature-first internal structure
- lightweight Clean Architecture principles inside modules
- reliable Outbox-backed integration events
- immediate consistency within modules
- eventual consistency across modules
