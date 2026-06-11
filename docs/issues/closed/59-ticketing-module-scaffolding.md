# Capability A: Module Scaffolding and Events Module Contract

## Summary

Establish the Ticketing module as a first-class participant in the system and define the cross-module contract that allows Ticketing to fetch manifest data from Events. This is the foundation for all subsequent Ticketing capabilities and the first synchronous cross-module interface in the codebase.

## Scope

- In scope:
  - `VenuePass.Modules.Ticketing` project with `TicketingDbContext`, `ticketing` schema, module registration
  - `VenuePass.Modules.Ticketing.Contracts` project (empty for now, structure placeholder)
  - Initial EF Core migration for Ticketing schema
  - `IEventsModuleContract` interface and `ManifestExport` DTOs in `Events.Contracts`
  - Internal implementation of `IEventsModuleContract` in Events module
  - DI registration of contract implementation
  - Unit test for manifest export (returns data for frozen, null for unfrozen)
- Out of scope:
  - Ticketing domain entities (Capability B)
  - Integration event handler wiring (Capability B)
  - Any Ticketing endpoints
  - Outbox table in Ticketing schema (deferred until Ticketing needs to publish)

## Dependencies

None — this is the first capability in M03.

## Acceptance Criteria

- [ ] `VenuePass.Modules.Ticketing` project exists, compiles, and is referenced by `VenuePass.Api`
- [ ] `TicketingDbContext` targets `ticketing` schema with a clean initial migration
- [ ] Ticketing module registers its DbContext and services via `ModuleConfiguration.cs`
- [ ] Ticketing module maps endpoints (empty for now) via `ModuleEndpointMappings.cs`
- [ ] `IEventsModuleContract` is declared in `Events.Contracts` with `GetManifestForTicketingAsync`
- [ ] `ManifestExportDto` and child DTOs are declared in `Events.Contracts`
- [ ] Events module implements `IEventsModuleContract` internally and registers it in DI
- [ ] Implementation returns manifest data only when manifest is frozen; returns `null` otherwise
- [ ] A unit test verifies frozen manifest returns correct export structure
- [ ] A unit test verifies unfrozen manifest returns `null`
- [ ] `dotnet build` and `dotnet test` pass at solution level
- [ ] Architecture tests pass (no boundary violations)

## Design Notes

### IEventsModuleContract

Lives in `Events.Contracts` alongside integration events:

```csharp
namespace VenuePass.Modules.Events.Contracts;

public interface IEventsModuleContract
{
    Task<ManifestExportDto?> GetManifestForTicketingAsync(
        Guid manifestId, CancellationToken ct);
}
```

### ManifestExport DTOs

```csharp
namespace VenuePass.Modules.Events.Contracts;

public sealed record ManifestExportDto(
    Guid ManifestId,
    Guid EventId,
    IReadOnlyList<SectionExportDto> Sections,
    IReadOnlyList<GeneralAdmissionAreaExportDto> GeneralAdmissionAreas);

public sealed record SectionExportDto(
    Guid SectionId,
    string Name,
    IReadOnlyList<RowExportDto> Rows);

public sealed record RowExportDto(
    Guid RowId,
    string Label,
    IReadOnlyList<SeatExportDto> Seats);

public sealed record SeatExportDto(
    Guid SeatId,
    string Label);

public sealed record GeneralAdmissionAreaExportDto(
    Guid AreaId,
    string Name,
    int Capacity);
```

### Internal Implementation (Events module)

```csharp
namespace VenuePass.Modules.Events.Infrastructure;

internal sealed class EventsModuleContract(EventsDbContext db) : IEventsModuleContract
{
    public async Task<ManifestExportDto?> GetManifestForTicketingAsync(
        Guid manifestId, CancellationToken ct)
    {
        var manifest = await db.Manifests
            .Include(m => m.Sections)
                .ThenInclude(s => s.Rows)
                    .ThenInclude(r => r.Seats)
            .Include(m => m.GeneralAdmissionAreas)
            .FirstOrDefaultAsync(m => m.Id == new ManifestId(manifestId), ct);

        if (manifest is null || !manifest.IsFrozen)
            return null;

        return MapToExport(manifest);
    }
}
```

### Registration

In Events `ModuleConfiguration`:

```csharp
services.AddScoped<IEventsModuleContract, EventsModuleContract>();
```

### Ticketing DbContext (minimal)

```csharp
public sealed class TicketingDbContext(DbContextOptions<TicketingDbContext> options)
    : DbContext(options)
{
    public const string Schema = "ticketing";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### Project References

```text
VenuePass.Modules.Ticketing         → BuildingBlocks.Messaging
                                    → Events.Contracts
VenuePass.Modules.Events            → Events.Contracts (already)
VenuePass.Api                       → Ticketing (new reference)
```

## Vertical Slices

- [ ] A1: Scaffold Ticketing module (project, DbContext, schema, migration, registration in Api host)
- [ ] A2: Add IEventsModuleContract and ManifestExport DTOs to Events.Contracts
- [ ] A3: Implement IEventsModuleContract in Events module with frozen-only guard and unit tests

## Risks and Assumptions

- Assumes manifest includes `IsFrozen` property accessible for the guard check (confirmed in M02 — `Manifest.Freeze()` sets this)
- `IEventsModuleContract` is resolved from the same DI container scope as the calling handler; since both modules are in-process this is straightforward, but the Ticketing handler's scope must be able to resolve Events' `EventsDbContext` transitively through the contract implementation
- The initial Ticketing migration is empty (schema only) — subsequent migrations will add tables as domain entities are implemented in Capability B

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
