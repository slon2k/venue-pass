# Architecture Plan — Milestone 04, Capability B: Reservation API and Expiration Flow

- **Requirements**: `docs/milestones/milestone-04.md` (Capability B section)
- **Architect**: DevTeam-Architect
- **Date**: 2026-06-12
- **Status**: Draft

---

## Design Summary

Capability B adds five slices on top of the domain and persistence completed in Capability A. Three of the slices are CRUD-style HTTP endpoints following the exact feature folder / handler / endpoint / validator pattern that already exists for `CreateOffer`, `GetOffer`, and `ActivateOffer`. The fourth slice is a command/handler pair for expiring a single reservation, and the fifth is a `BackgroundService` sweep that finds overdue reservations and calls that command. All inventory mutations (reserve, release) happen inside the same `SaveChangesAsync` call as the reservation state change, relying on the existing SQL Server `rowversion` concurrency tokens on `inventory_seats`, `inventory_pools`, and `reservations` to catch races. No new packages are needed. No new database tables are needed — the `reservations` and `reservation_items` tables already exist.

The plan is deliberately scoped to Capability B only. Capability C (checkout/orders) and Capability D (tickets) are not addressed here.

---

## Codebase Conventions Identified

| Aspect | Convention |
|---|---|
| **Language / Framework** | C# 13, .NET 10, ASP.NET Core Minimal APIs |
| **Project structure** | Vertical-slice feature folders: `Features/{FeatureName}/{FeatureName}.cs`, `{FeatureName}Endpoint.cs`, `{FeatureName}Errors.cs`, `{FeatureName}Validator.cs` |
| **Naming** | PascalCase classes, kebab-case columns, snake\_case table names |
| **Handler registration** | Explicit `services.AddScoped<XHandler>()` in `ModuleConfiguration.RegisterHandlers()` |
| **Endpoint registration** | Extension method `MapX(this IEndpointRouteBuilder)` called from `ModuleEndpointMappings.MapTicketingModule()` |
| **Result / error** | `Result<T>` railway type; `Error.NotFound`, `Error.Conflict`, `Error.Concurrency`; `ValidationError`; `ToProblem` extension maps to HTTP status |
| **Concurrency errors** | `Error.Concurrency(...)` maps to HTTP 409 via `ErrorHttpMappingExtensions` |
| **Validation** | FluentValidation `AbstractValidator<TCommand>` registered by assembly scan; handler runs `validator.ValidateAsync` first |
| **Domain exceptions** | Handlers catch `DomainRuleViolationException` and return the appropriate error; `ArgumentException` is also caught for value object guards |
| **Auth** | `RequireAuthorization("EventManager")` for manager-only endpoints; `RequireAuthorization()` (any authenticated role) for customer endpoints |
| **Background service** | `IHostedService` / `BackgroundService` pattern; tests replace all `IHostedService` with `TestNoopHostedService` by calling `services.RemoveAll<IHostedService>()` |
| **Configuration** | Read via `IConfiguration` in `ModuleConfiguration.AddTicketingModule`; `TimeProvider.System` already registered as singleton |
| **Test collection** | xUnit `[Collection("Events")]`; shared `EventsIntegrationTestFixture` via `ICollectionFixture`; SQL runs in Testcontainers MsSql |
| **Seed pattern** | `TicketingSeedHelpers` — static methods that call endpoints and assert 2xx; direct `TicketingDbContext` access for assertion/lookup |

### Reference Files

| Purpose | Reference File | Why |
|---|---|---|
| New command handler | `Features/ActivateOffer/ActivateOffer.cs` | Simplest mutation handler; no validator needed |
| New command handler with validator | `Features/CreateOffer/CreateOffer.cs` | Pattern for validator + domain call + DB save |
| New query handler | `Features/GetOffer/GetOffer.cs` | Read-only handler returning a result record |
| New endpoint (mutation, no body) | `Features/ActivateOffer/ActivateOfferEndpoint.cs` | 204 NoContent pattern |
| New endpoint (mutation, with body) | `Features/CreateOffer/CreateOfferEndpoint.cs` | FromBody request + 201 Created |
| New endpoint (query) | `Features/GetOffer/GetOfferEndpoint.cs` | 200 OK with `RequireAuthorization()` |
| Error constants | `Features/CreateOffer/CreateOfferErrors.cs` | Static class with typed factory methods |
| Validator | `Features/CreateOffer/CreateOfferValidator.cs` | FluentValidation `AbstractValidator<TCommand>` |
| Background service suppression in tests | `Infrastructure/EventsApiFactory.cs` | `services.RemoveAll<IHostedService>()` pattern |
| Seed helper extension | `Ticketing/Fixtures/TicketingSeedHelpers.cs` | Adding new seed methods |
| Integration test class | `Ticketing/Offers/ActivateOfferTests.cs` | xUnit class layout, client creation, assertion style |
| Auth test class | `Ticketing/Authorization/TicketingAuthorizationTests.cs` | How auth scenarios are structured |

---

## File Changes

### B1 — CreateReservation

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/CreateReservation/CreateReservationErrors.cs`

- **Purpose**: Typed error factory for all failure paths in CreateReservation.
- **Reference**: `Features/CreateOffer/CreateOfferErrors.cs`
- **Contents**:
  - `static Error OfferNotFound(Guid offerId)` → `Error.NotFound`
  - `static Error InventoryNotFound(Guid offerId)` → `Error.NotFound`
  - `static Error OfferNotActive()` → `Error.Conflict`
  - `static Error OfferNotOnSale()` → `Error.Conflict`
  - `static ValidationError InvalidData(IReadOnlyList<ValidationErrorDetail>)` → `ValidationError.Create`
  - `static ValidationError InvalidData(string message)` → single-detail overload
  - `static Error ConcurrencyConflict()` → `Error.Concurrency` (maps to 409; used when `DbUpdateConcurrencyException` is caught)
- **Depends on**: `VenuePass.BuildingBlocks.Application`
- **Imported by**: `CreateReservationHandler`

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/CreateReservation/CreateReservationValidator.cs`

- **Purpose**: FluentValidation rules for `CreateReservationCommand` before the handler runs.
- **Reference**: `Features/CreateOffer/CreateOfferValidator.cs`
- **Contents**: `AbstractValidator<CreateReservationCommand>` with:
  - `OfferId` must not be `Guid.Empty`
  - At least one of `SeatIds` or `GaPoolSelections` must be non-empty (either list may be empty as long as the total is > 0)
  - Each `SeatIds` element must not be `Guid.Empty`
  - Each `GaPoolSelections.PoolId` must not be `Guid.Empty`
  - Each `GaPoolSelections.Quantity` must be > 0
- **Depends on**: `FluentValidation`
- **Imported by**: DI assembly scan; `CreateReservationHandler` constructor

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/CreateReservation/CreateReservation.cs`

- **Purpose**: Handler, command, and result records for creating a reservation.
- **Reference**: `Features/CreateOffer/CreateOffer.cs` (validator + domain call pattern); `Features/ConfigurePricing/ConfigurePricing.cs` (loads Inventory alongside aggregate)
- **Contents**:

  ```
  CreateReservationCommand:
    Guid OfferId
    IReadOnlyList<Guid> SeatIds
    IReadOnlyList<GaPoolSelectionCommand> GaPoolSelections

  GaPoolSelectionCommand:
    Guid PoolId
    int Quantity

  CreateReservationResult:
    Guid ReservationId
    string Status
    DateTimeOffset ExpiresAt
    string Currency
    decimal Total
    IReadOnlyList<CreateReservationItemResult> Items

  CreateReservationItemResult:
    Guid ReservationItemId
    string Type                  // "Seat" or "GeneralAdmissionPool"
    Guid? InventorySeatId
    Guid? GeneralAdmissionPoolId
    Guid PriceZoneId
    int Quantity
    decimal UnitPrice
    decimal Total

  CreateReservationHandler(TicketingDbContext, IValidator<CreateReservationCommand>, IOptions<TicketingOptions>, TimeProvider, ILogger<CreateReservationHandler>):
    Handle(CreateReservationCommand, CancellationToken) → Task<Result<CreateReservationResult>>
  ```

- **Handler algorithm**:
  1. Run `validator.ValidateAsync` → return `InvalidData` on failure.
  2. Load `Offer` with `Include(o => o.PriceZones)` (tracked, not `AsNoTracking`) — needed so EF tracks it in the transaction.
  3. If offer is null → `OfferNotFound`.
  4. Load `Inventory` (tracked) with `.Include(i => i.Seats).Include(i => i.Pools)` — necessary for `ReserveSeats`/`ReserveGeneralAdmissionPool` mutations.
  5. Build `ReservationItemInventorySeatInput[]` from `command.SeatIds`.
  6. Build `ReservationItemGeneralAdmissionPoolInput[]` from `command.GaPoolSelections`.
  7. Compute `expiresAt = _timeProvider.GetUtcNow().Add(options.Value.ReservationExpiry)`.
  8. Call `Reservation.Create(offer, seatInputs, poolInputs, now, expiresAt)` inside try/catch for `DomainRuleViolationException` and `ArgumentException` → map to `InvalidData` or `OfferNotActive` / `OfferNotOnSale` based on error code.
  9. For each seat item in the created reservation, call `inventory.ReserveSeats([seatId])` inside the same try/catch.
  10. For each pool item in the created reservation, call `inventory.ReserveGeneralAdmissionPool(poolId, quantity)` inside the same try/catch.
  11. `db.Reservations.Add(reservation)`.
  12. Call `await db.SaveChangesAsync(ct)` inside a `try/catch(DbUpdateConcurrencyException)` → return `ConcurrencyConflict()`.
  13. Return `CreateReservationResult` mapped from the saved reservation.

- **Important note on loading strategy**: The `Inventory` aggregate owns seats and pools as navigations. Load with `Include(i => i.Seats).Include(i => i.Pools)` so that the change tracker sees both owned collections. Do not use `AsNoTracking`.

- **Depends on**: `TicketingDbContext`, `FluentValidation`, `TicketingOptions` (new, see Configuration section), `TimeProvider`, `Reservation`, `Inventory`
- **Imported by**: DI (`ModuleConfiguration`); `CreateReservationEndpoint`

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/CreateReservation/CreateReservationEndpoint.cs`

- **Purpose**: Maps `POST /reservations` and converts the request to a command.
- **Reference**: `Features/CreateOffer/CreateOfferEndpoint.cs`
- **Contents**:

  ```
  CreateReservationRequest:
    Guid OfferId
    IReadOnlyList<Guid> SeatIds
    IReadOnlyList<GaPoolSelectionRequest> GaPoolSelections

  GaPoolSelectionRequest:
    Guid PoolId
    int Quantity

  CreateReservationResponse:
    Guid ReservationId
    string Status
    DateTimeOffset ExpiresAt
    string Currency
    decimal Total
    IReadOnlyList<ReservationItemResponse> Items

  ReservationItemResponse:
    Guid ReservationItemId
    string Type
    Guid? InventorySeatId
    Guid? GeneralAdmissionPoolId
    Guid PriceZoneId
    int Quantity
    decimal UnitPrice
    decimal Total

  MapCreateReservation(this IEndpointRouteBuilder):
    app.MapPost("/reservations", Handle)
       .WithName("CreateReservation")
       .WithTags("Ticketing")
       .RequireAuthorization()       // any authenticated role
       .Produces<CreateReservationResponse>(201)
       .ProducesProblem(400)
       .ProducesProblem(404)
       .ProducesProblem(409)
       .ProducesProblem(500)
  ```

  - `ToCreated(result)` → `Results.Created($"/reservations/{result.ReservationId}", new CreateReservationResponse(...))`

---

### B2 — GetReservation

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/GetReservation/GetReservationErrors.cs`

- **Purpose**: Typed error factory.
- **Contents**: `static Error ReservationNotFound(Guid reservationId)` → `Error.NotFound`
- **Reference**: `Features/GetOffer/GetOfferErrors.cs`

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/GetReservation/GetReservation.cs`

- **Purpose**: Read-only handler returning full reservation details.
- **Reference**: `Features/GetOffer/GetOffer.cs`
- **Contents**:

  ```
  GetReservationQuery:
    Guid ReservationId

  GetReservationResult:
    Guid ReservationId
    Guid OfferId
    Guid InventoryId
    string Status
    DateTimeOffset ExpiresAt
    string Currency
    decimal Total
    IReadOnlyList<GetReservationItemResult> Items

  GetReservationItemResult:
    Guid ReservationItemId
    string Type
    Guid? InventorySeatId
    Guid? GeneralAdmissionPoolId
    Guid PriceZoneId
    int Quantity
    decimal UnitPrice
    decimal Total

  GetReservationHandler(TicketingDbContext):
    Handle(GetReservationQuery, CancellationToken) → Task<Result<GetReservationResult>>
  ```

- **Handler algorithm**:
  1. `db.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == new ReservationId(query.ReservationId), ct)`
  2. If null → `ReservationNotFound`.
  3. Map and return `GetReservationResult` including all items.

- **Note**: The `Items` navigation is owned and will be loaded automatically by EF Core when the parent is loaded via `AsNoTracking()` because it is configured as `OwnsMany`. No explicit `Include` is required — verify this against the `ReservationConfiguration` which uses `OwnsMany(r => r.Items, ...)` and `UsePropertyAccessMode(PropertyAccessMode.Field)`.

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/GetReservation/GetReservationEndpoint.cs`

- **Purpose**: Maps `GET /reservations/{reservationId}`.
- **Reference**: `Features/GetOffer/GetOfferEndpoint.cs`
- **Contents**:

  ```
  MapGetReservation(this IEndpointRouteBuilder):
    app.MapGet("/reservations/{reservationId:guid}", Handle)
       .WithName("GetReservation")
       .WithTags("Ticketing")
       .RequireAuthorization()
       .Produces<GetReservationResult>(200)
       .ProducesProblem(404)
       .ProducesProblem(500)
  ```

---

### B3 — CancelReservation

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/CancelReservation/CancelReservationErrors.cs`

- **Purpose**: Typed error factory.
- **Contents**:
  - `static Error ReservationNotFound(Guid id)` → `Error.NotFound`
  - `static Error ReservationNotCancellable(Guid id)` → `Error.Conflict` — for terminal-state rejections
  - `static Error ConcurrencyConflict()` → `Error.Concurrency`
- **Reference**: `Features/CreateOffer/CreateOfferErrors.cs`

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/CancelReservation/CancelReservation.cs`

- **Purpose**: Handler and command for cancelling a reservation and releasing its inventory.
- **Reference**: `Features/ActivateOffer/ActivateOffer.cs` (no validator needed; mutation on aggregate)
- **Contents**:

  ```
  CancelReservationCommand:
    Guid ReservationId

  CancelReservationResult: (empty record)

  CancelReservationHandler(TicketingDbContext, ILogger<CancelReservationHandler>):
    Handle(CancelReservationCommand, CancellationToken) → Task<Result<CancelReservationResult>>
  ```

- **Handler algorithm**:
  1. Load `Reservation` (tracked) by ID. If null → `ReservationNotFound`.
  2. Load `Inventory` (tracked) with `Include(i => i.Seats).Include(i => i.Pools)` using `reservation.InventoryId`.
  3. Call `reservation.Cancel()` inside try/catch `DomainRuleViolationException` → `ReservationNotCancellable`.
  4. Release inventory for each reservation item (see "Inventory Release Pattern" below).
  5. `await db.SaveChangesAsync(ct)` inside try/catch `DbUpdateConcurrencyException` → `ConcurrencyConflict`.
  6. Return `CancelReservationResult`.

- **Inventory release pattern** (reused in B4 as well):
  - Collect all `InventorySeatId` values from items where `Type == Seat`. If any → call `inventory.ReleaseSeats(seatIds)`.
  - For each item where `Type == GeneralAdmissionPool` → call `inventory.ReleaseGeneralAdmissionPool(poolId, quantity)`.

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/CancelReservation/CancelReservationEndpoint.cs`

- **Purpose**: Maps `DELETE /reservations/{reservationId}`.
- **Reference**: `Features/ActivateOffer/ActivateOfferEndpoint.cs`
- **Contents**:

  ```
  MapCancelReservation(this IEndpointRouteBuilder):
    app.MapDelete("/reservations/{reservationId:guid}", Handle)
       .WithName("CancelReservation")
       .WithTags("Ticketing")
       .RequireAuthorization()
       .Produces(204)
       .ProducesProblem(404)
       .ProducesProblem(409)
       .ProducesProblem(500)
  ```

  - On success → `Results.NoContent()`

---

### B4 / B5 — Expiration Command + Background Sweep Worker

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/ExpireReservation/ExpireReservationErrors.cs`

- **Purpose**: Typed error factory for expiration.
- **Contents**:
  - `static Error ReservationNotFound(Guid id)` → `Error.NotFound`
  - `static Error ReservationNotExpirable(Guid id)` → `Error.Conflict`
  - `static Error ConcurrencyConflict()` → `Error.Concurrency`
- **Reference**: `Features/CancelReservation/CancelReservationErrors.cs`

#### [CREATE] `src/VenuePass.Modules.Ticketing/Features/ExpireReservation/ExpireReservation.cs`

- **Purpose**: Handler and command for expiring a single reservation and releasing its inventory.
- **Reference**: `Features/CancelReservation/CancelReservation.cs`
- **Contents**:

  ```
  ExpireReservationCommand:
    Guid ReservationId

  ExpireReservationResult: (empty record)

  ExpireReservationHandler(TicketingDbContext, TimeProvider, ILogger<ExpireReservationHandler>):
    Handle(ExpireReservationCommand, CancellationToken) → Task<Result<ExpireReservationResult>>
  ```

- **Handler algorithm**:
  1. Load `Reservation` (tracked) by ID. If null → `ReservationNotFound`.
  2. Load `Inventory` (tracked) with `Include(i => i.Seats).Include(i => i.Pools)` using `reservation.InventoryId`.
  3. Call `reservation.Expire(_timeProvider.GetUtcNow())` inside try/catch `DomainRuleViolationException` → `ReservationNotExpirable`. This call will throw if the reservation is not in `Reserved` status or if `ExpiresAt > now`.
  4. Release inventory using the same inventory release pattern as CancelReservation.
  5. `await db.SaveChangesAsync(ct)` inside try/catch `DbUpdateConcurrencyException` → `ConcurrencyConflict`.
  6. Return `ExpireReservationResult`.

- **No endpoint**: This command is not exposed as an HTTP endpoint. It is called only by the background sweep worker. This is intentional — do not add an endpoint for it.

#### [CREATE] `src/VenuePass.Modules.Ticketing/Infrastructure/ReservationExpirationWorker.cs`

- **Purpose**: `BackgroundService` that periodically finds overdue `Reserved` reservations and expires them.
- **Reference**: There is no existing background service in the Ticketing module. The `EventsOutboxDispatcher` in the Events module (`Infrastructure/Outbox/EventsOutboxDispatcher.cs`) is the closest analogue — read it to understand the `IHostedService` pattern used in the codebase. `TestNoopHostedService` in `EventsApiFactory` shows how the test harness replaces all hosted services.
- **Contents**:

  ```
  ReservationExpirationWorker(IServiceScopeFactory, ILogger<ReservationExpirationWorker>, IOptions<TicketingOptions>):
    ExecuteAsync(CancellationToken stoppingToken) — override
  ```

- **Worker algorithm**:
  1. Loop until `stoppingToken` is cancelled, sleeping `options.Value.ExpirationSweepInterval` (e.g. 60 seconds) between runs via `Task.Delay`.
  2. On each iteration, open a new `IServiceScope`, resolve `TicketingDbContext` and `ExpireReservationHandler`.
  3. Query: `db.Reservations.AsNoTracking().Where(r => r.Status == ReservationStatus.Reserved && r.ExpiresAt < now).Select(r => r.Id.Value).ToListAsync(ct)`.
  4. For each candidate ID, call `await handler.Handle(new ExpireReservationCommand(id), ct)`.
  5. If the result is failure (e.g. `ConcurrencyConflict` or `ReservationNotExpirable` because a concurrent checkout already completed it), log at `Information` level and continue. Do not rethrow.
  6. If an unhandled exception occurs on an individual reservation, log at `Warning` and continue to the next one. Do not crash the loop.
  7. Catch exceptions at the outer loop level (log at `Error`) and continue the sleep-iterate cycle.

- **Scope management**: Each sweep iteration must create a new `IServiceScope` so that the scoped `TicketingDbContext` does not accumulate state across runs.

---

### Configuration

#### [CREATE] `src/VenuePass.Modules.Ticketing/TicketingOptions.cs`

- **Purpose**: Strongly-typed options class for the Ticketing module.
- **Contents**:

  ```csharp
  public sealed class TicketingOptions
  {
      public const string SectionName = "Ticketing";
      public int ReservationExpiryMinutes { get; init; } = 15;
      public TimeSpan ReservationExpiry => TimeSpan.FromMinutes(ReservationExpiryMinutes);
      public TimeSpan ExpirationSweepInterval { get; init; } = TimeSpan.FromMinutes(1);
  }
  ```

- **Note**: `ExpirationSweepInterval` is not required to be configurable by the milestone spec, but making it a property with a default of 1 minute avoids hardcoding and aids test control without extra complexity.

#### [MODIFY] `src/VenuePass.Modules.Ticketing/ModuleConfiguration.cs`

- **Purpose**: Register new handlers, worker, and options.
- **Changes**:
  1. Add `services.Configure<TicketingOptions>(configuration.GetSection(TicketingOptions.SectionName));` in `AddTicketingModule`.
  2. Add `services.AddHostedService<ReservationExpirationWorker>();` in `AddTicketingModule`.
  3. Add `services.AddScoped<CreateReservationHandler>();` in `RegisterHandlers`.
  4. Add `services.AddScoped<GetReservationHandler>();` in `RegisterHandlers`.
  5. Add `services.AddScoped<CancelReservationHandler>();` in `RegisterHandlers`.
  6. Add `services.AddScoped<ExpireReservationHandler>();` in `RegisterHandlers`.
  7. Add `using VenuePass.Modules.Ticketing.Infrastructure;` (for `ReservationExpirationWorker`) and using statements for all new feature namespaces.
- **Lines affected**: ~15 lines in `AddTicketingModule` and `RegisterHandlers`.

#### [MODIFY] `src/VenuePass.Modules.Ticketing/ModuleEndpointMappings.cs`

- **Purpose**: Register the three new HTTP endpoints.
- **Changes**:
  1. `app.MapCreateReservation();`
  2. `app.MapGetReservation();`
  3. `app.MapCancelReservation();`
  4. Add `using` statements for the three new feature namespaces.
- **Lines affected**: ~10 lines.

#### [MODIFY] `src/VenuePass.Api/appsettings.Development.json`

- **Purpose**: Add default development configuration for `Ticketing:ReservationExpiryMinutes`.
- **Changes**: Add a `"Ticketing"` section with `"ReservationExpiryMinutes": 15`.
- **Note**: `appsettings.json` (production defaults) should not contain environment-specific values; the `TicketingOptions` default of 15 minutes already provides the production default without needing an entry in `appsettings.json`.

---

### Integration Tests

#### [CREATE] `tests/VenuePass.IntegrationTests/Ticketing/Reservations/CreateReservationTests.cs`

- **Purpose**: Happy-path and rejection tests for B1 (covers E1, E2 from milestone).
- **Reference**: `Ticketing/Offers/CreateOfferTests.cs`
- **Collection**: `[Collection(EventsTestCollectionFixture.Name)]`
- See Test Strategy section for scenario list.

#### [CREATE] `tests/VenuePass.IntegrationTests/Ticketing/Reservations/GetReservationTests.cs`

- **Purpose**: Tests for B2.
- **Reference**: `Ticketing/Offers/GetOfferTests.cs`

#### [CREATE] `tests/VenuePass.IntegrationTests/Ticketing/Reservations/CancelReservationTests.cs`

- **Purpose**: Tests for B3 and B5 (inventory released on cancel, covers E3).
- **Reference**: `Ticketing/Offers/ActivateOfferTests.cs`

#### [CREATE] `tests/VenuePass.IntegrationTests/Ticketing/Reservations/ExpireReservationTests.cs`

- **Purpose**: Tests for B4 and B5 (expiration command releases inventory, covers E3).
- **Note**: The background sweep worker is suppressed in all tests via `EventsApiFactory`'s `services.RemoveAll<IHostedService>()`. Expiration is tested by calling the `ExpireReservationHandler` directly via the DI container, not via the sweep loop.

#### [CREATE] `tests/VenuePass.IntegrationTests/Ticketing/Reservations/ReservationConcurrencyTests.cs`

- **Purpose**: Double-reservation and race condition tests (covers E5).
- **Note**: Requires parallel HTTP calls on the same seat or pool to prove concurrency protection. See "Concurrency Test Approach" in the Risk section.

#### [MODIFY] `tests/VenuePass.IntegrationTests/Ticketing/Fixtures/TicketingSeedHelpers.cs`

- **Purpose**: Add seed helpers for reservation tests.
- **Changes**: Add methods:
  - `CreateReservationAsync(HttpClient, Guid offerId, IReadOnlyList<Guid> seatIds, IReadOnlyList<(Guid poolId, int qty)> gaPools)` → `Guid reservationId`
  - `GetInventorySeatIdsAsync(EventsIntegrationTestFixture fixture, Guid eventId)` → `List<Guid>` (already exists in per-test private helpers; extract to shared)
  - `GetInventoryPoolIdsAsync(EventsIntegrationTestFixture fixture, Guid eventId)` → `List<Guid>`
  - `SetupActiveOfferAsync(EventsIntegrationTestFixture fixture, HttpClient managerClient, Guid eventId)` → `Guid offerId` (creates offer, configures all seats+pools in one price zone, activates)

#### [MODIFY] `tests/VenuePass.IntegrationTests/Ticketing/Authorization/TicketingAuthorizationTests.cs`

- **Purpose**: Add auth enforcement tests for the three new endpoints (covers E7).
- **Changes**: Add three test methods:
  - `CreateReservation_WhenUnauthenticated_Returns401`
  - `GetReservation_WhenUnauthenticated_Returns401`
  - `CancelReservation_WhenUnauthenticated_Returns401`

---

## Interfaces and Types

### TicketingOptions

- **Location**: `src/VenuePass.Modules.Ticketing/TicketingOptions.cs`
- **Purpose**: Strongly typed options bound from `"Ticketing"` configuration section.

```
Properties:
  ReservationExpiryMinutes: int — default 15; controls ExpiresAt = now + this
  ReservationExpiry: TimeSpan — computed from ReservationExpiryMinutes
  ExpirationSweepInterval: TimeSpan — default 60s; controls how often the sweep runs
```

### CreateReservationCommand

- **Location**: `Features/CreateReservation/CreateReservation.cs`
- **Purpose**: Input to `CreateReservationHandler`.

```
Properties:
  OfferId: Guid
  SeatIds: IReadOnlyList<Guid>
  GaPoolSelections: IReadOnlyList<GaPoolSelectionCommand>

GaPoolSelectionCommand:
  PoolId: Guid
  Quantity: int
```

### CreateReservationResult

- **Location**: `Features/CreateReservation/CreateReservation.cs`
- **Purpose**: Returned to the endpoint on success.

```
Properties:
  ReservationId: Guid
  Status: string          // always "Reserved" on creation
  ExpiresAt: DateTimeOffset
  Currency: string        // 3-letter ISO code
  Total: decimal
  Items: IReadOnlyList<CreateReservationItemResult>

CreateReservationItemResult:
  ReservationItemId: Guid
  Type: string            // "Seat" or "GeneralAdmissionPool"
  InventorySeatId: Guid?
  GeneralAdmissionPoolId: Guid?
  PriceZoneId: Guid
  Quantity: int
  UnitPrice: decimal
  Total: decimal
```

### GetReservationResult

- **Location**: `Features/GetReservation/GetReservation.cs`
- **Purpose**: Response payload for `GET /reservations/{id}`.

```
Properties:
  ReservationId: Guid
  OfferId: Guid
  InventoryId: Guid
  Status: string
  ExpiresAt: DateTimeOffset
  Currency: string
  Total: decimal
  Items: IReadOnlyList<GetReservationItemResult>

GetReservationItemResult: (same fields as CreateReservationItemResult)
```

---

## Data Flow

### B1 — CreateReservation

```
1. Client sends POST /reservations { offerId, seatIds[], gaPoolSelections[] }
2. CreateReservationEndpoint maps body to CreateReservationCommand
3. CreateReservationHandler:
   a. FluentValidation validates command
   b. Load Offer (tracked, include PriceZones) from TicketingDbContext
   c. Load Inventory (tracked, include Seats + Pools) from TicketingDbContext
   d. Call Reservation.Create(...) — validates offer status/sale window, resolves prices, builds items
   e. Call Inventory.ReserveSeats([...]) for each seat item
   f. Call Inventory.ReserveGeneralAdmissionPool(...) for each pool item
   g. db.Reservations.Add(reservation)
   h. db.SaveChangesAsync() — single transaction writes: reservation row, reservation_items rows,
      updated inventory_seats.availability, updated inventory_pools.reserved_count
      If DbUpdateConcurrencyException → return ConcurrencyConflict error (HTTP 409)
4. Return 201 Created with CreateReservationResponse
```

**Error paths**:
- Validation failure → 400
- Offer not found → 404
- Offer not active / not on sale → 409 (Conflict)
- Seat not covered by offer price zone → 400 (domain rule: `InvalidData`)
- Seat not available → 409 (Conflict mapped from `DomainRuleViolationException` → `InvalidData` which maps to 400; alternatively if we want cleaner semantics, detect `InventoryErrors.SeatNotAvailable` code and map to Conflict — see Open Questions #1)
- GA pool quantity > available → 409
- Duplicate seats or pools in request → 400
- Concurrency conflict on inventory row → 409

### B3 — CancelReservation

```
1. Client sends DELETE /reservations/{reservationId}
2. CancelReservationEndpoint extracts reservationId from route
3. CancelReservationHandler:
   a. Load Reservation (tracked)
   b. Load Inventory (tracked, include Seats + Pools)
   c. reservation.Cancel() — throws if not in Reserved status
   d. inventory.ReleaseSeats([seatIds from items]) — if any seat items
   e. inventory.ReleaseGeneralAdmissionPool(poolId, qty) — for each pool item
   f. db.SaveChangesAsync() — single transaction: reservation.status = Cancelled,
      inventory_seats.availability = Available, inventory_pools.reserved_count -= qty
      Concurrency conflict → ConcurrencyConflict error (HTTP 409)
4. Return 204 No Content
```

### B4 — ExpireReservation (command path)

```
Same structure as CancelReservation, with differences:
- Command: ExpireReservationCommand(ReservationId)
- Domain call: reservation.Expire(now) — also checks ExpiresAt <= now
- Error for terminal state: ReservationNotExpirable
- Not an endpoint; only called by the worker or directly in tests
```

### B4 — Background Sweep Worker

```
Every ExpirationSweepInterval (default 60s):
1. Create new IServiceScope
2. Resolve TicketingDbContext (scoped) and ExpireReservationHandler (scoped)
3. Query: SELECT id FROM reservations WHERE status = 'Reserved' AND expires_at < now
4. For each id:
   call ExpireReservationHandler.Handle(new ExpireReservationCommand(id), ct)
   If result is failure (concurrency, already-expired) → log Info, continue
5. Dispose scope
6. Delay ExpirationSweepInterval
```

---

## Transaction and Concurrency Design

### Single-transaction guarantee

Every state-changing command (CreateReservation, CancelReservation, ExpireReservation) loads the `Reservation` and `Inventory` aggregates as tracked EF Core entities, mutates them in memory via domain methods, and calls a single `db.SaveChangesAsync()`. EF Core writes all changes (reservation row + inventory rows) in one SQL transaction.

### Concurrency tokens

All three relevant tables carry SQL Server `rowversion` columns mapped as EF Core concurrency tokens:
- `ticketing.reservations.row_version`
- `ticketing.inventory_seats.row_version`
- `ticketing.inventory_pools.row_version`

When two concurrent requests attempt to modify the same row, one will succeed and the other will receive a `DbUpdateConcurrencyException`. The losing request returns `Error.Concurrency(...)` which maps to HTTP 409 via `ErrorHttpMappingExtensions`.

### Race: CreateReservation vs CreateReservation (same seat)

Both load the same `InventorySeat` row at `Available`. Both call `seat.Reserve()`. The first to commit wins; the second hits the `rowversion` concurrency check and receives `DbUpdateConcurrencyException` → 409.

### Race: CancelReservation vs ExpireReservation (same reservation)

Both load the reservation at `Reserved`. The first to commit changes `status` to `Cancelled` or `Expired`. When the second calls `SaveChangesAsync`, the `reservations.row_version` has changed → `DbUpdateConcurrencyException` → 409. No double-release of inventory is possible.

### Race: ExpireReservation (worker) vs CheckoutReservation (Capability C, future)

The same `reservations.row_version` concurrency token protects this race. Whichever transaction commits first wins. The loser receives a concurrency error and returns it to the caller. The checkout path (C2) must handle `Error.Concurrency` as a first-class outcome and surface it as 409.

### GA pool double-reservation

The `inventory_pools.row_version` token is set at the pool row level. If two concurrent requests try to reserve quantities from the same pool, only one can commit per `rowversion` cycle. However, note that if two requests targeting *different* seats on the *same* inventory aggregate are submitted concurrently, EF Core's unit-of-work will detect a conflict only if the same `inventory_seats` row is touched. Two requests targeting *different* seats can succeed concurrently — this is correct behavior and not a bug.

### Why not an explicit database transaction?

EF Core's `SaveChangesAsync` already wraps all pending changes in a single SQL transaction. An explicit `BeginTransactionAsync` is not needed and would add unnecessary complexity. The existing pattern (`ActivateOffer`, `ConfigurePricing`) confirms that a single `SaveChangesAsync` is the project convention.

---

## Expiration Worker Design

### Class: `ReservationExpirationWorker` (BackgroundService)

**Constructor parameters**:
- `IServiceScopeFactory scopeFactory` — for creating per-run scoped services
- `ILogger<ReservationExpirationWorker> logger`
- `IOptions<TicketingOptions> options`

**ExecuteAsync pseudocode**:
```
while (!stoppingToken.IsCancellationRequested):
  try:
    await RunSweepAsync(stoppingToken)
  catch OperationCanceledException:
    break
  catch Exception ex:
    logger.LogError(ex, "Unhandled error in expiration sweep; will retry next interval")
  await Task.Delay(options.Value.ExpirationSweepInterval, stoppingToken)

RunSweepAsync(CancellationToken ct):
  using scope = scopeFactory.CreateScope()
  db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>()
  handler = scope.ServiceProvider.GetRequiredService<ExpireReservationHandler>()
  now = (scope.ServiceProvider.GetRequiredService<TimeProvider>()).GetUtcNow()
  candidateIds = await db.Reservations
      .AsNoTracking()
      .Where(r => r.Status == ReservationStatus.Reserved && r.ExpiresAt < now)
      .Select(r => r.Id.Value)
      .ToListAsync(ct)
  foreach id in candidateIds:
    try:
      result = await handler.Handle(new ExpireReservationCommand(id), ct)
      if result.IsFailure:
        logger.LogInformation("Could not expire reservation {Id}: {Code}", id, result.Error.Code)
    catch Exception ex:
      logger.LogWarning(ex, "Error while expiring reservation {Id}", id)
```

**Why per-reservation calls instead of a bulk SQL UPDATE?**

A bulk UPDATE would bypass domain logic (`Reservation.Expire`) and inventory release (`Inventory.ReleaseSeats`, `Inventory.ReleaseGeneralAdmissionPool`). The milestone spec explicitly states the background sweep must call "the expiration command path" to keep the inventory mutation consistent.

**Test isolation**: The `EventsApiFactory` calls `services.RemoveAll<IHostedService>()` in tests, which removes `ReservationExpirationWorker`. This is already the established pattern. Tests that want to verify expiration behaviour call `ExpireReservationHandler` directly through the DI container.

---

## Configuration

### Reading `Ticketing:ReservationExpiryMinutes`

```
appsettings.Development.json:
{
  "Ticketing": {
    "ReservationExpiryMinutes": 15
  }
}
```

`TicketingOptions` is bound in `ModuleConfiguration.AddTicketingModule`:
```csharp
services.Configure<TicketingOptions>(configuration.GetSection(TicketingOptions.SectionName));
```

`CreateReservationHandler` receives `IOptions<TicketingOptions>` via constructor injection. `ReservationExpirationWorker` receives the same.

`TimeProvider.System` is already registered as a singleton in `ModuleConfiguration`. Both the handler and the worker use `_timeProvider.GetUtcNow()` for all current-time checks, which makes time injectable for unit tests if needed in the future.

---

## Endpoint Routing

| Slice | Method | Path | Auth | Success Status |
|---|---|---|---|---|
| B1 CreateReservation | POST | `/reservations` | Any authenticated role (`RequireAuthorization()`) | 201 Created |
| B2 GetReservation | GET | `/reservations/{reservationId:guid}` | Any authenticated role | 200 OK |
| B3 CancelReservation | DELETE | `/reservations/{reservationId:guid}` | Any authenticated role | 204 No Content |
| B4 ExpireReservation | — | (no HTTP endpoint) | N/A | N/A |

**Rationale for POST `/reservations`** (not `/offers/{offerId}/reservations`):
The offer ID is part of the request body, not the path. The milestone doc shows the request shape as `{ offerId, seatIds[], gaPools[] }`. A top-level resource URL (`/reservations`) is consistent with how `CreateOffer` uses `/events/{eventId}/offers` where the parent ID is in the path only when it provides scoping clarity. Either approach is defensible; this plan follows the top-level pattern which mirrors future `GET /reservations/{id}` symmetry.

---

## Testing Strategy

| Component | Test Type | Test File | Key Scenarios |
|---|---|---|---|
| CreateReservation | Integration | `Reservations/CreateReservationTests.cs` | See E1, E2 below |
| GetReservation | Integration | `Reservations/GetReservationTests.cs` | 200 OK happy path; 404 unknown ID; auth |
| CancelReservation | Integration | `Reservations/CancelReservationTests.cs` | Cancel reserved → 204 + inventory released; cancel completed/expired → 409; auth |
| ExpireReservation | Integration | `Reservations/ExpireReservationTests.cs` | Expire reserved past ExpiresAt → inventory released; expire not-yet-expired → 409; expire already-cancelled → 409 |
| Concurrency | Integration | `Reservations/ReservationConcurrencyTests.cs` | See E5 below |
| Auth enforcement | Integration | `Authorization/TicketingAuthorizationTests.cs` | 401 for all three endpoints unauthenticated |

### E1 — Reservation Flow Tests (`CreateReservationTests.cs`)

1. `CreateReservation_WithAvailableSeat_Returns201WithCorrectShape` — create reservation against active offer with one seat; assert 201, body has ReservationId, Status=Reserved, ExpiresAt is in the future, item has correct UnitPrice from price zone, Total correct.
2. `CreateReservation_WithAvailableGaPool_Returns201` — reserve GA pool quantity.
3. `CreateReservation_WithMixedSeatAndPool_Returns201` — reserve one seat and one pool in same request.
4. `CreateReservation_ReducesInventoryAvailability` — after reservation, call `GET /events/{id}/inventory` and assert seat count / pool count reflects the reservation.

### E2 — Rejection Tests (`CreateReservationTests.cs`)

5. `CreateReservation_WithInactiveOffer_Returns409` — offer in Draft status.
6. `CreateReservation_WithExpiredSaleWindow_Returns409` — offer active but `SaleEnd` in the past.
7. `CreateReservation_WithFutureSaleWindow_Returns409` — offer active but `SaleStart` in the future.
8. `CreateReservation_WithSeatNotCoveredByPriceZone_Returns400` — seat exists in inventory but not in any price zone.
9. `CreateReservation_WithUnavailableSeat_Returns409` — seat already reserved.
10. `CreateReservation_WithGaQuantityAboveAvailable_Returns409` — request quantity > pool available.
11. `CreateReservation_WithDuplicateSeatIds_Returns400` — same seat ID twice in request.
12. `CreateReservation_WithDuplicatePoolIds_Returns400` — same pool ID twice.
13. `CreateReservation_WithUnknownOfferId_Returns404`.
14. `CreateReservation_WhenUnauthenticated_Returns401`.

### E3 — Expiration/Cancellation Release Tests

`CancelReservationTests.cs`:
1. `CancelReservation_ReleasesInventorySeat` — reserve seat, cancel, assert seat shows as available again.
2. `CancelReservation_ReleasesGaPool` — reserve pool, cancel, assert pool available count restored.
3. `CancelReservation_OnExpiredReservation_Returns409` — expire first, then try cancel.
4. `CancelReservation_OnAlreadyCancelled_Returns409`.
5. `CancelReservation_WhenUnauthenticated_Returns401`.

`ExpireReservationTests.cs`:
1. `ExpireReservation_WhenPastExpiresAt_ReleasesInventory` — set `ExpiresAt` to past by creating reservation with a very short expiry (requires `TicketingOptions` override in test factory), then call handler directly.
2. `ExpireReservation_WhenNotYetExpired_Returns409`.
3. `ExpireReservation_WhenAlreadyCancelled_Returns409`.

**Test approach for expiry-window control**: In expiration tests, override `TicketingOptions` via `configureTestServices` in `EventsApiFactory` to set `ReservationExpiryMinutes` to a very small value (e.g. 0 minutes, or use `TimeProvider` replacement). Alternatively, resolve `ExpireReservationHandler` directly and call it after manually manipulating `ExpiresAt` in the database using the `TicketingDbContext` from a scope.

The simpler approach — and the one consistent with the current test pattern — is:
1. Create a reservation normally.
2. Open a scope, resolve `TicketingDbContext`, set `reservation.ExpiresAt` to `now.AddMinutes(-1)` via a direct EF update (`ExecuteUpdateAsync`), save.
3. Resolve `ExpireReservationHandler` and call `Handle`.
4. Assert result is success and inventory is released.

This avoids needing a fake `TimeProvider` for B4/B5 tests.

### E5 — Concurrency Tests (`ReservationConcurrencyTests.cs`)

1. `ConcurrentReservation_ForSameSeat_OnlyOneSucceeds` — two parallel `CreateReservation` calls for the same seat; assert exactly one returns 201 and the other returns 409.
2. `ConcurrentReservation_ForSameGaPool_WithExcessQuantity_PreventsOversell` — two requests each requesting the full pool capacity; assert one succeeds and total reserved quantity does not exceed capacity.

**Implementation approach**: Use `Task.WhenAll` with two `HttpClient` instances from the same fixture factory. Both send identical requests simultaneously. Assert the outcomes sum to one success and one failure.

### E7 — Authorization Tests (additions to `TicketingAuthorizationTests.cs`)

1. `CreateReservation_WhenUnauthenticated_Returns401`
2. `GetReservation_WhenUnauthenticated_Returns401`
3. `CancelReservation_WhenUnauthenticated_Returns401`

---

## Dependencies

### New Packages

None required. `Microsoft.Extensions.Options` is already transitively available via ASP.NET Core. `Microsoft.Extensions.Hosting.Abstractions` (for `BackgroundService`) is also already in the ASP.NET Core meta-package.

### Existing Dependencies Leveraged

- `FluentValidation.DependencyInjectionExtensions` — already registered; assembly scan picks up `CreateReservationValidator`.
- `Microsoft.EntityFrameworkCore.SqlServer` — already used; `DbUpdateConcurrencyException` is in `Microsoft.EntityFrameworkCore`.
- `TimeProvider.System` — already registered as singleton in `ModuleConfiguration`.
- `IOptions<T>` — already used in the codebase pattern (see `ModuleConfiguration` for the `AddAuthentication` pattern).

---

## Risks

| # | Risk | Severity | Likelihood | Mitigation |
|---|---|---|---|---|
| 1 | **Inventory not loaded with full owned collections**: `Inventory` owns `Seats` and `Pools` via `OwnsMany`. If loaded without `Include(i => i.Seats).Include(i => i.Pools)`, the collection is empty and `ReserveSeats`/`ReleaseSeats` silently do nothing or throw a "seat not found" error rather than a concurrency error. | High | Medium | All handlers that mutate inventory must explicitly `Include` both owned collections. Add a test that verifies seat availability changes after reservation. |
| 2 | **Seat-not-available error maps as 400 instead of 409**: `Inventory.ReserveSeats` throws `DomainRuleViolationException` with code `InventoryErrors.SeatNotAvailable`. The handler catches `DomainRuleViolationException` and maps it via `InvalidData` → 400. This may be confusing for clients: a seat being unavailable is a conflict (someone else has it), not a validation error. | Medium | High | See Open Questions #1. Decide whether to detect the specific error code and map to `Error.Conflict` instead of `InvalidData`. |
| 3 | **Background sweep consumes excess concurrency errors on busy events**: If 1000 reservations expire simultaneously, the sweep fires 1000 `ExpireReservationHandler` calls serially. Under heavy load this could be slow. | Low | Low | Acceptable for M04 per decision #23 (no advanced retry/backoff). Note in docs. |
| 4 | **Reservation item `OwnsMany` lazy-loading gap**: EF Core's `AsNoTracking` with `OwnsMany` should load owned items automatically in a single query, but this behaviour depends on the query shape. Verify that `GetReservationHandler` returns items without requiring an explicit `Include`. | Medium | Low | Add an assertion in `GetReservationTests` that the Items collection is populated on the response. |
| 5 | **Test clock control for expiry**: Tests for `ExpireReservation` must set `ExpiresAt` to the past. The direct-EF-update approach (set `ExpiresAt` via `ExecuteUpdateAsync`) bypasses the domain aggregate and concurrency token, which is acceptable in test setup code. | Low | Medium | Use `ExecuteUpdateAsync` specifically to avoid domain object updates in setup. Document this approach in the test file. |
| 6 | **`CreateReservation` path: `Reservation.Create` builds items internally, but inventory mutation happens in the handler**: The handler must call `inventory.ReserveSeats`/`ReserveGeneralAdmissionPool` after `Reservation.Create`. The items are already created by the domain. The handler must extract the seat IDs and pool IDs+quantities from `reservation.Items` to feed back to `inventory.ReserveSeats`. This coupling is by design (handler coordinates domain objects) but must be correct. | Medium | Low | The handler iterates `reservation.Items` and calls the appropriate inventory method for each. Covered by integration test E1 #4 which asserts inventory availability decreases. |
| 7 | **`CreateReservation` with `SaleStart`/`SaleEnd` null**: The `Offer.SalesRange` is a `DateTimeRange` which can have null start/end (unbounded). If `DateTimeRange.Contains(now)` treats null as "always valid", creation will succeed even with no sale window set — which is the desired behavior for offers without a sale window. Verify this by reading `ValueObjects.cs`. | Low | Low | Confirm `DateTimeRange.Contains` behavior before implementing, or add a unit test for this case. |

---

## Scope Assessment

| Category | Count |
|---|---|
| Files Created (source) | 11 |
| Files Modified (source) | 2 |
| Files Created (tests) | 5 |
| Files Modified (tests) | 2 |
| **Total files changed** | **20** |

**PR Size**: Medium (20 files, all within one module)

**Split Recommended**: No — all changes are cohesive within the Ticketing module. The three endpoints (B1–B3), the expiration command (B4), the worker (B4), and the inventory release (B5) are tightly interdependent (cancel/expire share the same inventory release pattern; tests for B5 depend on B3/B4 handlers existing).

### Prerequisites

- Capability A must be merged (all domain types, EF configs, migrations, and `DbSet<Reservation>` are required). This is already confirmed as complete.
- The `docs/plans` directory must exist (created by this plan; no action needed before implementation).
- No infrastructure changes needed (no new environment variables beyond the optional `Ticketing:ReservationExpiryMinutes`).

---

## Open Questions

- [ ] **OQ1**: Should `Inventory.ReserveSeats` throwing `SeatNotAvailable` map to `Error.Conflict` (HTTP 409) or to `InvalidData` / `Error.Validation` (HTTP 400)? The current `DomainRuleViolationException` catch in handlers maps everything to `InvalidData` which produces 400. A seat being taken by someone else is semantically a conflict. Decision affects both `CreateReservation` and how other handlers deal with domain state errors. Recommendation: detect the specific error code (`InventoryErrors.SeatNotAvailable.Code`) in the handler and return `Error.Conflict` instead.

- [ ] **OQ2**: Should the `ExpireReservation` command be exposed as an internal HTTP endpoint (e.g. accessible only from tests or admin tools via a protected route) to simplify testing? Currently the plan calls the handler directly from tests via the DI container. The direct-DI approach is consistent with how the team tests handlers elsewhere (see `GetInventoryStatusHandlerTests.cs`) and does not require exposing an endpoint. Confirm this approach is acceptable.

- [ ] **OQ3**: The `CreateReservation` request uses a top-level `POST /reservations` route rather than `POST /offers/{offerId}/reservations`. Confirm the preferred URL convention. The current plan uses top-level because the offer ID is semantically an input, not a parent resource scope — but both are defensible.

- [ ] **OQ4**: `TicketingOptions.ExpirationSweepInterval` defaults to 60 seconds. Should this be surfaced in `appsettings.Development.json` for visibility, or left as a code default? Adding it to the config file documents it but also adds a config file change that could be confusing if not noted in the PR description.

- [ ] **OQ5**: The `BackgroundService` sweep calls `ExpireReservationHandler` once per candidate ID serially. For correctness in M04 this is fine, but under high concurrency (many expired reservations) this could slow the sweep. Confirm serial processing is acceptable for M04, with a note that parallelisation is a future concern.
