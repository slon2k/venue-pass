using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.Modules.Events.Contracts.IntegrationEvents;
using VenuePass.Modules.Events.Infrastructure;
using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.EventSync;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class EventSyncTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public EventSyncTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Sync_WhenEventPublished_CreatesPublishedEventReferenceInTicketing()
    {
        var (eventId, factory) = await PublishAndDispatchAsync();
        await using (factory)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var ticketingDb = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

            var reference = await ticketingDb.PublishedEventReferences
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.EventId == eventId);

            Assert.NotNull(reference);
            Assert.Equal(eventId, reference!.EventId);
        }
    }

    [Fact]
    public async Task Sync_WhenEventPublished_CreatesInventoryWithCorrectSeatCount()
    {
        // The manifest template used by the seed helper has 1 section, 1 row, 2 seats.
        var (eventId, factory) = await PublishAndDispatchAsync();
        await using (factory)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var ticketingDb = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

            var reference = await ticketingDb.PublishedEventReferences
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.EventId == eventId);

            Assert.NotNull(reference);

            var inventory = await ticketingDb.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.EventReferenceId == reference!.Id);

            Assert.NotNull(inventory);
            Assert.Equal(2, inventory!.Seats.Count);
        }
    }

    [Fact]
    public async Task Sync_WhenEventPublished_CreatesInventoryWithCorrectPoolCount()
    {
        // The manifest template used by the seed helper has 1 GA area.
        var (eventId, factory) = await PublishAndDispatchAsync();
        await using (factory)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var ticketingDb = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

            var reference = await ticketingDb.PublishedEventReferences
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.EventId == eventId);

            Assert.NotNull(reference);

            var inventory = await ticketingDb.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.EventReferenceId == reference!.Id);

            Assert.NotNull(inventory);
            Assert.Single(inventory!.Pools);
        }
    }

    [Fact]
    public async Task Sync_WhenSameEventPublishedTwice_DoesNotCreateDuplicateInventory()
    {
        await using EventsApiFactory factory = _fixture.CreateFactory(enableOutboxDispatcher: true);

        using HttpClient adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventAdmin");

        string managerId = Guid.NewGuid().ToString();
        using HttpClient managerClient = factory.CreateClient();
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, managerId);
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventManager");

        Guid venueId = await CreateVenueAsync(adminClient);
        Guid templateId = await CreateManifestTemplateAsync(adminClient, venueId);
        Guid eventId = await CreateEventAsync(managerClient, venueId, templateId);

        // First publish + dispatch
        HttpResponseMessage firstPublish = await managerClient.PostAsync($"/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.NoContent, firstPublish.StatusCode);

        await WaitForOutboxProcessedAsync(factory, eventId);

        // Attempt a second publish (will be 409 — idempotency check)
        // The outbox handler should still be idempotent and not create a duplicate reference.
        await managerClient.PostAsync($"/events/{eventId}/publish", null);

        await WaitForOutboxCountAsync(factory, eventId, minProcessedCount: 1);

        await using var scope = factory.Services.CreateAsyncScope();
        var ticketingDb = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        int referenceCount = await ticketingDb.PublishedEventReferences
            .AsNoTracking()
            .CountAsync(r => r.EventId == eventId);

        Assert.Equal(1, referenceCount);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new factory with the outbox dispatcher enabled, seeds a published event,
    /// and waits for the Ticketing handler to process it.
    /// Returns the eventId and the factory (caller must dispose the factory).
    /// </summary>
    private async Task<(Guid EventId, EventsApiFactory Factory)> PublishAndDispatchAsync()
    {
        EventsApiFactory factory = _fixture.CreateFactory(enableOutboxDispatcher: true);

        using HttpClient adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventAdmin");

        string managerId = Guid.NewGuid().ToString();
        using HttpClient managerClient = factory.CreateClient();
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, managerId);
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventManager");

        Guid venueId = await CreateVenueAsync(adminClient);
        Guid templateId = await CreateManifestTemplateAsync(adminClient, venueId);
        Guid eventId = await CreateEventAsync(managerClient, venueId, templateId);

        HttpResponseMessage publishResponse = await managerClient.PostAsync($"/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.NoContent, publishResponse.StatusCode);

        await WaitForOutboxProcessedAsync(factory, eventId);

        return (eventId, factory);
    }

    private static async Task WaitForOutboxProcessedAsync(EventsApiFactory factory, Guid eventId)
    {
        await WaitUntilAsync(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            var candidates = await db.OutboxMessages
                .AsNoTracking()
                .Where(m => m.Type == typeof(EventPublishedIntegrationEvent).AssemblyQualifiedName)
                .OrderByDescending(m => m.OccurredOn)
                .ToListAsync();

            return candidates.Any(m =>
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<EventPublishedIntegrationEvent>(m.Payload);
                    return payload?.EventId == eventId && m.ProcessedOn is not null;
                }
                catch (JsonException)
                {
                    return false;
                }
            });
        }, timeout: TimeSpan.FromSeconds(15));
    }

    private static async Task WaitForOutboxCountAsync(EventsApiFactory factory, Guid eventId, int minProcessedCount)
    {
        await WaitUntilAsync(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            var candidates = await db.OutboxMessages
                .AsNoTracking()
                .Where(m => m.Type == typeof(EventPublishedIntegrationEvent).AssemblyQualifiedName)
                .OrderByDescending(m => m.OccurredOn)
                .ToListAsync();

            int processedCount = candidates.Count(m =>
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<EventPublishedIntegrationEvent>(m.Payload);
                    return payload?.EventId == eventId && m.ProcessedOn is not null;
                }
                catch (JsonException)
                {
                    return false;
                }
            });

            return processedCount >= minProcessedCount;
        }, timeout: TimeSpan.FromSeconds(15));
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan timeout)
    {
        var start = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - start < timeout)
        {
            if (await predicate())
            {
                return;
            }

            await Task.Delay(200);
        }

        throw new Xunit.Sdk.XunitException($"Condition was not satisfied within {timeout.TotalSeconds} seconds.");
    }

    // -------------------------------------------------------------------------
    // Private request / response records
    // -------------------------------------------------------------------------

    private static async Task<Guid> CreateVenueAsync(HttpClient adminClient)
    {
        CreateVenueRequest request = new(
            Name: $"Venue {Guid.NewGuid()}",
            Address: "123 Main St",
            City: "Seattle",
            Country: "US",
            Capacity: 500);

        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/events/venues", request);
        CreateVenueResponse? body = await response.Content.ReadFromJsonAsync<CreateVenueResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        return body!.VenueId;
    }

    private static async Task<Guid> CreateManifestTemplateAsync(HttpClient adminClient, Guid venueId)
    {
        CreateManifestTemplateRequest request = new(
            Name: $"Template {Guid.NewGuid()}",
            Description: "Concert layout",
            VenueId: venueId,
            Sections:
            [
                new CreateManifestTemplateSectionRequest(
                    Name: "Floor",
                    Rows:
                    [
                        new CreateManifestTemplateRowRequest(
                            Label: "A",
                            Seats:
                            [
                                new CreateManifestTemplateSeatRequest("1"),
                                new CreateManifestTemplateSeatRequest("2")
                            ])
                    ])
            ],
            GeneralAdmissionAreas:
            [
                new CreateManifestTemplateGeneralAdmissionAreaRequest(
                    Name: "GA East",
                    Capacity: 300)
            ]);

        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/events/manifest-templates", request);
        CreateManifestTemplateResponse? body = await response.Content.ReadFromJsonAsync<CreateManifestTemplateResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        return body!.ManifestTemplateId;
    }

    private static async Task<Guid> CreateEventAsync(HttpClient managerClient, Guid venueId, Guid templateId)
    {
        CreateEventRequest request = new(
            VenueId: venueId,
            ManifestTemplateId: templateId,
            Name: $"Event {Guid.NewGuid()}",
            EventDate: DateTimeOffset.UtcNow.AddMonths(2),
            Description: "Event sync test");

        HttpResponseMessage response = await managerClient.PostAsJsonAsync("/events", request);
        CreateEventResponse? body = await response.Content.ReadFromJsonAsync<CreateEventResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        return body!.EventId;
    }

    private sealed record CreateVenueRequest(
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    private sealed record CreateVenueResponse(
        Guid VenueId,
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    private sealed record CreateManifestTemplateRequest(
        string Name,
        string? Description,
        Guid VenueId,
        IReadOnlyList<CreateManifestTemplateSectionRequest> Sections,
        IReadOnlyList<CreateManifestTemplateGeneralAdmissionAreaRequest> GeneralAdmissionAreas);

    private sealed record CreateManifestTemplateSectionRequest(
        string Name,
        IReadOnlyList<CreateManifestTemplateRowRequest> Rows);

    private sealed record CreateManifestTemplateRowRequest(
        string Label,
        IReadOnlyList<CreateManifestTemplateSeatRequest> Seats);

    private sealed record CreateManifestTemplateSeatRequest(string Label);

    private sealed record CreateManifestTemplateGeneralAdmissionAreaRequest(string Name, int Capacity);

    private sealed record CreateManifestTemplateResponse(Guid ManifestTemplateId);

    private sealed record CreateEventRequest(
        Guid VenueId,
        Guid ManifestTemplateId,
        string Name,
        DateTimeOffset EventDate,
        string? Description);

    private sealed record CreateEventResponse(Guid EventId, Guid ManifestId, Guid AssignedManagerId);
}
