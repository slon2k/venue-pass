# VenuePass — Technical Decisions

Each decision is recorded with context, choice, rationale, and tradeoffs accepted.

---

## TD-01: Command/Query Dispatch — Direct Injection

**Context:** Modular monolith needs a pattern for executing use cases from API endpoints.

**Choice:** Direct service injection. Each feature defines a co-located `Command` (or `Query`) + `Handler` class. The handler is registered in DI and injected directly into the endpoint.

**Example:**

```csharp
// Features/CreateVenue/CreateVenue.cs
public static class CreateVenue
{
    public sealed record Command(string Name, string City, int Capacity);
    public sealed record Response(Guid VenueId);

    public sealed class Handler(EventsDbContext db)
    {
        public async Task<Result<Response>> Handle(Command command, CancellationToken ct)
        {
            var venue = Venue.Create(command.Name, command.City, command.Capacity);
            db.Venues.Add(venue);
            await db.SaveChangesAsync(ct);
            return new Response(venue.Id);
        }
    }
}

// Features/CreateVenue/CreateVenueEndpoint.cs
app.MapPost("/events/venues", async (
    CreateVenueRequest request,
    CreateVenue.Handler handler,
    CancellationToken ct) =>
{
    var command = new CreateVenue.Command(
        request.Name,
        request.City,
        request.Capacity);

    var result = await handler.Handle(command, ct);

    return result.IsSuccess
        ? Results.Created($"/events/venues/{result.Value.VenueId}", new CreateVenueResponse(result.Value.VenueId))
        : result.ToProblemResult();
});
```

**Rationale:**

- Explicit, traceable call chains — no hidden pipeline magic
- Easy to test: `new Handler(deps)` with mocked dependencies
- Easy to navigate: handler lives next to its endpoint
- Validation handled explicitly in handler or via endpoint filters
- Transactions managed at DbContext/module level

**Tradeoffs accepted:**

- No generic pipeline behaviors (logging, validation, timing) — added per-feature if needed
- Cross-cutting concerns require explicit wiring rather than automatic decoration
- Acceptable for a project with clear, well-defined handlers

---

## TD-02: Cross-Module Events — Hand-Rolled Outbox

**Context:** Modules must communicate state changes reliably without distributed transactions.

**Choice:** Hand-rolled Outbox pattern using EF Core + a background `IHostedService` dispatcher.

**Design:**

- Each module has an `OutboxMessage` table in its own schema
- Writing business state + outbox message happens in the same `SaveChanges` transaction
- A background service polls for unsent messages and dispatches them in-process
- Subscriber handlers are resolved via DI and invoked directly
- Delivery is at-least-once; handlers must be idempotent

**Rationale:**

- No external broker dependency for a demo project
- Keeps the system fully in-process while practicing production-grade patterns
- Simple enough to debug; complex enough to be realistic

**Tradeoffs accepted:**

- No built-in retry policies, dead-letter queues, or observability — added incrementally
- No message ordering guarantees beyond single-message atomicity
- Acceptable for a demo project; a library (MassTransit, Wolverine) would be appropriate for production

---

## TD-03: API Style — Minimal APIs with Endpoint Classes

**Context:** Need a clean way to expose HTTP endpoints per feature.

**Choice:** ASP.NET Minimal APIs. Each feature has its own endpoint file that maps one route.

**Rationale:**

- Co-located with the handler — one folder per feature contains everything
- No controller bloat — each endpoint is independent
- Straightforward request binding and response mapping
- Endpoint filters available for cross-cutting concerns (validation, error handling)

**Tradeoffs accepted:**

- No automatic grouping/tagging from controller attributes — use `.WithTags()` explicitly
- Swagger/OpenAPI metadata requires explicit `.WithName()`, `.Produces<T>()` calls

---

## TD-04: Persistence — EF Core with Schema-Per-Module

**Context:** Modular monolith with strict module boundaries needs isolated persistence.

**Choice:**

- One SQL Server database
- One schema per module (`events`, `ticketing`, `attendance`, `identity`)
- One `DbContext` per module, configured for its own schema
- Per-module migration history

**Rationale:**

- Schema separation enforces ownership at the database level
- Single database simplifies local dev and deployment
- Per-module DbContext prevents accidental cross-module joins
- Architecture tests can verify no cross-schema references in configurations

**Tradeoffs accepted:**

- Schema-level isolation is weaker than separate databases
- Shared DB means a module *could* technically query another's tables (enforced by convention + tests, not by infrastructure)

---

## TD-05: Database — SQL Server in Docker

**Context:** Need a local development database that supports schemas natively.

**Choice:** SQL Server 2022 via `mcr.microsoft.com/mssql/server:2022-latest` in Docker Compose.

**Rationale:**

- Native schema support (unlike SQLite)
- Production-realistic behavior
- Familiar in .NET ecosystem
- Docker Compose makes setup reproducible and disposable

**Tradeoffs accepted:**

- Requires Docker Desktop running
- Heavier than LocalDB or SQLite for quick iteration

---

## TD-06: Identity — Graduated Approach

**Context:** Identity is needed for role-based access but shouldn't block domain module development.

**Choice:** Two-phase approach:

| Phase | How |
|-------|-----|
| During Events/Ticketing/Attendance slices | `dotnet user-jwts` generates dev tokens with `sub`, `role` claims |
| Identity slice | Custom module with hand-rolled user/role tables, password hashing, login endpoint, JWT issuance |

**Rationale:**

- Domain modules only care about JWT claims — they don't know or care who issued the token
- `dotnet user-jwts` is zero-code and gives valid tokens immediately
- Custom Identity module keeps the boundary clean without ASP.NET Identity's opinionated baggage
- Swappable to Keycloak / Entra ID later — module contracts don't change

**Tradeoffs accepted:**

- No production-grade features (lockout, email confirmation, MFA) in MVP
- Hand-rolled password hashing requires care (use `Microsoft.AspNetCore.Identity.PasswordHasher<T>` or similar)
- Acceptable for a demo — real auth can be layered on without domain changes

---

## TD-07: Manifest Modeling — Full Template Chain in MVP

**Context:** Section 22 of the reference doc introduces ManifestTemplate → Manifest → Inventory layering. This adds complexity to the Events module but provides realistic domain modeling.

**Choice:** Include the full template/snapshot chain in MVP.

**Scope:**

- `Venue` has one or more `ManifestTemplate`s
- `Event` gets a `Manifest` (snapshot from a chosen template)
- `Manifest` is locked on event publication
- Ticketing creates `Inventory` from the locked `Manifest`

**Rationale:**

- This is the most architecturally interesting part of the Events module
- It exercises real domain modeling problems (immutability, snapshots, lifecycle transitions)
- It creates a meaningful boundary between Events and Ticketing

**Tradeoffs accepted:**

- Events module is significantly more complex than a flat "create event" CRUD
- First slice takes longer but produces higher learning value
- Manifest template CRUD + snapshot logic is the bulk of the Events module

---

## TD-08: Testing — Architecture Tests for Boundary Enforcement

**Context:** Module boundaries are the core architectural constraint. They must be verified automatically.

**Choice:** Dedicated `VenuePass.ArchitectureTests` project using NetArchTest (or ArchUnitNET) to enforce:

- `Domain` code does not depend on `Infrastructure`
- modules do not depend on another module’s internals
- cross-module access is allowed only through public contracts
- modules do not reference another module’s `DbContext`, entities, handlers, or infrastructure code

**Rationale:**

- Rules that are not tested will be violated
- Architecture tests catch boundary violations early
- They document the architectural intent as executable specifications

**Additional testing:**

- Unit tests for domain logic and invariants
- Integration tests for persistence and outbox behavior
- End-to-end tests for critical flows added later by slice

---

## TD-09: CI/CD Strategy — Progressive Maturity

**Context:** CI must provide early safety without slowing down initial development.

**Choice:** Progressive CI/CD rollout with two CI workflows (`ci`, `integration-ci`) aligned with delivery slices.

**Stages:**

- M00-M02 baseline `ci`: restore, build, solution-level tests (including architecture and unit tests)
- M03-M05 add `integration-ci`: SQL Server service container integration tests, migration smoke checks, outbox integration tests
- M06-M07 optional release automation: Docker image build, artifact publish, demo deployment workflow with manual approval

**Policy:**

- `ci` is required on all pull requests from M00 onward
- `integration-ci` becomes required as soon as the integration suite is stable (target: by M06)
- Release workflow stays manual-approval gated

**Rationale:**

- Keeps early feedback fast and reliable
- Introduces heavier checks only when persistence and module integration become central
- Avoids over-engineering release automation before the system stabilizes

**Tradeoffs accepted:**

- Early stages do not validate full container image builds
- Deployment automation is postponed until later slices
- CI runtime will increase from M03 onward due to integration test matrix

---

## TD-10: Cross-Module Synchronization — Notify and Fetch

**Context:** Some module state changes must trigger work in another module, but the full source-of-truth data remains owned by the publishing module.

**Choice:** Publish a thin integration event containing identifiers only. If the consumer needs more data, it fetches that data through the owning module’s public contract and materializes its own local model.

**Example:**

- `Events` publishes `EventPublished(EventId, ManifestId)`
- `Ticketing` receives the event
- `Ticketing` calls `IEventsModule.GetManifestForTicketingAsync(...)`
- `Ticketing` creates `Inventory`

**Rationale:**

- Keeps integration events small and stable
- Preserves ownership boundaries
- Avoids sharing domain entities across modules
- Lets each module build and own its own local representation

**Tradeoffs accepted:**

- Adds an extra read call after event reception
- Requires idempotent synchronization logic in the consumer
- Acceptable because ownership and clarity are more important than minimizing one additional in-process call

---

## Decision Index

| ID | Topic | Choice |
|----|-------|--------|
| TD-01 | Dispatch | Direct injection, co-located Command + Handler |
| TD-02 | Cross-module events | Hand-rolled Outbox (EF Core + background service) |
| TD-03 | API style | Minimal APIs with endpoint classes |
| TD-04 | Persistence | EF Core, schema-per-module, one DbContext per module |
| TD-05 | Database | SQL Server 2022 in Docker |
| TD-06 | Identity | Graduated: `dotnet user-jwts` → custom Identity module |
| TD-07 | Manifest modeling | Full template → snapshot chain in MVP |
| TD-08 | Testing | Architecture tests + per-module unit/integration tests |
| TD-09 | CI/CD | Progressive maturity: baseline CI → integration CI → optional release automation |
| TD-10 | Cross-Module Synchronization | Notify and Fetch |
