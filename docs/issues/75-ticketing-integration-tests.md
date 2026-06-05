# Capability E: Integration Tests

## Summary

Validate the Ticketing module's end-to-end behavior in a real database-backed test environment, covering cross-module event synchronization, offer CRUD lifecycle, and inventory query accuracy. These tests prove that module wiring, persistence, authorization, and cross-module contract invocation work correctly together.

## Scope

- In scope:
  - Integration tests for event sync path (EventPublished → inventory created in Ticketing)
  - Idempotency verification (duplicate messages produce no duplicate state)
  - Integration tests for offer creation, pricing configuration, activation, and query
  - Integration tests for inventory status endpoint accuracy
  - Authorization enforcement tests (role checks, unauthenticated rejection)
  - Test infrastructure: multi-module test host, seed helpers, auth token helpers
- Out of scope:
  - Unit tests for domain invariants (covered in Capabilities B and C)
  - Outbox dispatcher unit tests (covered in M02)
  - Performance or load testing
  - Negative edge cases exhaustively covered in unit tests (poison messages, unresolvable types)

## Dependencies

- Capabilities B, C, and D complete (domain, handlers, and endpoints implemented)
- M02 integration test infrastructure available (WebApplicationFactory, Testcontainers or equivalent)

## Acceptance Criteria

### Event Sync Tests

- [ ] Publishing an event in Events module results in `PublishedEventReference` and `Inventory` existing in Ticketing schema
- [ ] Inventory contains correct number of seats matching the manifest
- [ ] Inventory contains correct number of GA pools matching the manifest
- [ ] Duplicate `EventPublished` dispatch does not create duplicate inventory
- [ ] Manifest fetch failure (simulated) results in message not processed (available for retry)

### Offer CRUD Tests

- [ ] `POST /events/{eventId}/offers` with valid payload returns 201 and creates a Draft offer
- [ ] `POST /events/{eventId}/offers` for non-published event returns 404
- [ ] `PUT /offers/{offerId}/price-levels` with valid price levels returns 200 and persists levels
- [ ] `PUT /offers/{offerId}/price-levels` targeting non-existent seat IDs returns 400
- [ ] `PUT /offers/{offerId}/price-levels` on Active offer returns 400/409
- [ ] `POST /offers/{offerId}/activate` with price levels configured returns 200 and state becomes Active
- [ ] `POST /offers/{offerId}/activate` without price levels returns 400
- [ ] `POST /offers/{offerId}/activate` on already-active offer returns 409
- [ ] `GET /offers/{offerId}` returns offer with price levels and state
- [ ] `GET /events/{eventId}/offers` returns list of offers for the event

### Inventory Status Tests

- [ ] `GET /events/{eventId}/inventory` returns correct section-level seat counts
- [ ] `GET /events/{eventId}/inventory` returns correct pool capacity and available counts
- [ ] `GET /events/{eventId}/inventory` for non-published event returns 404

### Authorization Tests

- [ ] Mutation endpoints reject unauthenticated requests with 401
- [ ] Mutation endpoints reject non-EventManager role with 403
- [ ] Query endpoints reject unauthenticated requests with 401
- [ ] Query endpoints succeed for any authenticated user

### Infrastructure

- [ ] Tests run against a real database (SQL Server via Testcontainers or equivalent)
- [ ] Multi-module test host boots both Events and Ticketing modules
- [ ] Tests are deterministic and independent (no shared mutable state between tests)
- [ ] `dotnet test` passes

## Design Notes

### Test Host Setup

Extend existing `WebApplicationFactory` from M02 to include Ticketing module registration:

```csharp
public class VenuePassApiFactory : WebApplicationFactory<Program>
{
    // Both Events and Ticketing modules registered via Program.cs
    // Test database provisioned per test class or per test run
}
```

### Event Sync Test Strategy

Rather than waiting for the background dispatcher, invoke `DispatchBatchAsync` directly in the test:

```csharp
[Fact]
public async Task Sync_WhenEventPublished_CreatesInventoryInTicketing()
{
    // Arrange — create and publish an event via Events API
    var eventId = await PublishEvent();

    // Act — trigger outbox dispatch directly
    await DispatchOutboxMessages();

    // Assert — verify Ticketing state
    await using var scope = Factory.Services.CreateAsyncScope();
    var ticketingDb = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

    var reference = await ticketingDb.PublishedEventReferences
        .FirstOrDefaultAsync(r => r.EventId == eventId);

    Assert.NotNull(reference);

    var inventory = await ticketingDb.Inventories
        .Include(i => i.Seats)
        .Include(i => i.Pools)
        .FirstOrDefaultAsync(i => i.EventReferenceId == reference.Id);

    Assert.NotNull(inventory);
    Assert.True(inventory.Seats.Count > 0);
}
```

### Dispatch Helper

```csharp
private async Task DispatchOutboxMessages()
{
    await using var scope = Factory.Services.CreateAsyncScope();
    var dispatcher = scope.ServiceProvider.GetRequiredService<EventsOutboxDispatcher>();
    await dispatcher.DispatchBatchAsync(CancellationToken.None);
}
```

Requires `InternalsVisibleTo` from Events module to the integration test project — already established in M02.

### Seed Helpers

Shared helper methods to reduce test boilerplate:

```csharp
// Creates venue + manifest template + event + publishes it
private async Task<Guid> PublishEvent()
{
    var venueId = await CreateVenue();
    var templateId = await CreateManifestTemplate(venueId);
    var eventId = await CreateEvent(venueId, templateId);
    await PublishEvent(eventId);
    return eventId;
}

// Triggers outbox dispatch so Ticketing inventory is ready
private async Task<Guid> PublishEventAndSyncInventory()
{
    var eventId = await PublishEvent();
    await DispatchOutboxMessages();
    return eventId;
}

// Creates offer for a synced event
private async Task<Guid> CreateOfferForEvent(Guid eventId, string name = "Standard")
{
    var response = await Client.PostAsJsonAsync(
        $"/events/{eventId}/offers",
        new { Name = name });
    // extract and return offer ID
}
```

### Test Organization

```text
tests/
└── VenuePass.Modules.Ticketing.IntegrationTests/
    ├── EventSync/
    │   └── EventSyncTests.cs
    ├── Offers/
    │   ├── CreateOfferTests.cs
    │   ├── ConfigurePricingTests.cs
    │   ├── ActivateOfferTests.cs
    │   └── GetOfferTests.cs
    ├── Inventory/
    │   └── GetInventoryStatusTests.cs
    ├── Authorization/
    │   └── TicketingAuthorizationTests.cs
    └── Fixtures/
        ├── TicketingApiFactory.cs
        └── SeedHelpers.cs
```

### Data Isolation Strategy

Each test class uses its own database (Testcontainers spins up fresh container per class) or tests create unique entities with unique IDs so they don't conflict. Prefer per-class isolation for simplicity.

## Vertical Slices

- [ ] E1: Set up multi-module integration test project and fixtures (test host, seed helpers, dispatch helper)
- [ ] E2: Integration tests for event sync path (inventory creation and idempotency)
- [ ] E3: Integration tests for offer CRUD (create, configure pricing, activate, get)
- [ ] E4: Integration tests for inventory status endpoint
- [ ] E5: Authorization enforcement tests across Ticketing endpoints

## Risks and Assumptions

- Multi-module test host requires both Events and Ticketing to be registered and healthy; a regression in Events could break Ticketing integration tests
- Direct `DispatchBatchAsync` invocation assumes `InternalsVisibleTo` is configured for the integration test project (established pattern from M02)
- Database seed operations (create venue, template, event, publish) add test execution time; shared seed fixtures per class can mitigate but add coupling between tests
- Testcontainers requires Docker to be available in CI; verify CI environment supports this before relying on it
- Tests that cross module boundaries (Events → Outbox → Ticketing) have a larger failure surface; failures may require investigation across modules

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Tests passing
- [ ] Docs updated if behavior changed
