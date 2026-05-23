# Development Guide

## EF Core migrations from the repository root

Run all commands from the repository root (`venue-pass`).

### 1) Add a new migration

Use this command to create a migration for the Events module:

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/VenuePass.Modules.Events `
  --startup-project src/VenuePass.Api `
  --context EventsDbContext `
  --output-dir Infrastructure/Migrations
```

Example:

```powershell
dotnet ef migrations add AddVenueCapacity `
  --project src/VenuePass.Modules.Events `
  --startup-project src/VenuePass.Api `
  --context EventsDbContext `
  --output-dir Infrastructure/Migrations
```

Ticketing example (planned module wiring):

```powershell
dotnet ef migrations add AddInventorySeed `
  --project src/VenuePass.Modules.Ticketing `
  --startup-project src/VenuePass.Api `
  --context TicketingDbContext `
  --output-dir Infrastructure/Migrations
```

Attendance example (planned module wiring):

```powershell
dotnet ef migrations add AddCheckInAttempt `
  --project src/VenuePass.Modules.Attendance `
  --startup-project src/VenuePass.Api `
  --context AttendanceDbContext `
  --output-dir Infrastructure/Migrations
```

### 2) Apply migrations (update database)

Use this command to apply pending migrations to the database:

```powershell
dotnet ef database update `
  --project src/VenuePass.Modules.Events `
  --startup-project src/VenuePass.Api `
  --context EventsDbContext
```

Ticketing example (planned module wiring):

```powershell
dotnet ef database update `
  --project src/VenuePass.Modules.Ticketing `
  --startup-project src/VenuePass.Api `
  --context TicketingDbContext
```

Attendance example (planned module wiring):

```powershell
dotnet ef database update `
  --project src/VenuePass.Modules.Attendance `
  --startup-project src/VenuePass.Api `
  --context AttendanceDbContext
```

## Notes

- If `dotnet ef` is not available, install it first:

```powershell
dotnet tool install --global dotnet-ef
```

- To target a specific migration:

```powershell
dotnet ef database update <MigrationName> `
  --project src/VenuePass.Modules.Events `
  --startup-project src/VenuePass.Api `
  --context EventsDbContext
```
