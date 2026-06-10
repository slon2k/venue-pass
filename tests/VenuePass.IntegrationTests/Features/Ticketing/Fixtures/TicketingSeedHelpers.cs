using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.Modules.Events.Contracts.IntegrationEvents;
using VenuePass.Modules.Events.Infrastructure;
using VenuePass.Modules.Events.IntegrationTests.Infrastructure;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Features.Ticketing.Fixtures;

/// <summary>
/// Shared seed helpers used across all Ticketing integration test classes.
/// All methods assert the HTTP responses were successful before returning IDs.
/// </summary>
internal static class TicketingSeedHelpers
{
    /// <summary>
    /// Creates a venue, manifest template, event, publishes it, and waits for
    /// the outbox dispatcher to process the message so the Ticketing module's
    /// EventPublishedHandler runs and inventory is created. Returns the eventId.
    /// </summary>
    public static async Task<Guid> PublishEventAndSyncInventoryAsync(
        EventsIntegrationTestFixture fixture,
        HttpClient managerClient)
    {
        // Use a factory with the outbox dispatcher enabled so the EventPublishedHandler
        // in the Ticketing module processes the integration event synchronously during setup.
        await using EventsApiFactory factory = fixture.CreateFactory(enableOutboxDispatcher: true);

        using HttpClient adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventAdmin");

        // Re-create a manager client against the same factory so all requests share the same DI scope.
        string managerId = Guid.NewGuid().ToString();
        using HttpClient factoryManagerClient = factory.CreateClient();
        factoryManagerClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, managerId);
        factoryManagerClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventManager");

        Guid venueId = await CreateVenueAsync(adminClient);
        Guid templateId = await CreateManifestTemplateAsync(adminClient, venueId);
        Guid eventId = await CreateEventAsync(factoryManagerClient, venueId, templateId);

        HttpResponseMessage publishResponse = await factoryManagerClient.PostAsync($"/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.NoContent, publishResponse.StatusCode);

        // Wait for the outbox dispatcher to process the EventPublished message, which
        // triggers the Ticketing EventPublishedHandler and creates the inventory.
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
        }, timeout: TimeSpan.FromSeconds(12));

        return eventId;
    }

    /// <summary>POST /events/{eventId}/offers — returns offerId.</summary>
    public static async Task<Guid> CreateOfferAsync(
        HttpClient managerClient,
        Guid eventId,
        string name = "Standard",
        string currency = "USD")
    {
        CreateOfferRequest request = new(
            Name: name,
            Currency: currency,
            SaleStart: null,
            SaleEnd: null);

        HttpResponseMessage response = await managerClient.PostAsJsonAsync($"/events/{eventId}/offers", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateOfferResponse? body = await response.Content.ReadFromJsonAsync<CreateOfferResponse>();
        Assert.NotNull(body);

        return body!.OfferId;
    }

    /// <summary>PUT /offers/{offerId}/price-zones — expects 204 No Content.</summary>
    public static async Task ConfigurePriceZonesAsync(
        HttpClient managerClient,
        Guid offerId,
        IReadOnlyList<PriceZoneItem> zones)
    {
        ConfigurePricingRequest request = new(
            PriceZones: [.. zones.Select(z => new PriceZoneRequestItem(
                Name: z.Name,
                Price: z.Price,
                SeatIds: [.. z.SeatIds],
                PoolIds: [.. z.PoolIds]))]);

        HttpResponseMessage response = await managerClient.PutAsJsonAsync($"/offers/{offerId}/price-zones", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>POST /offers/{offerId}/activate — expects 204 No Content.</summary>
    public static async Task ActivateOfferAsync(HttpClient managerClient, Guid offerId)
    {
        HttpResponseMessage response = await managerClient.PostAsync($"/offers/{offerId}/activate", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Internal helpers for setting up the test event (venue, template, event)
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
        // Minimal template: 1 section, 1 row, 2 seats, 1 GA area — keeps seat IDs predictable.
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
            Description: "Ticketing integration test event");

        HttpResponseMessage response = await managerClient.PostAsJsonAsync("/events", request);
        CreateEventResponse? body = await response.Content.ReadFromJsonAsync<CreateEventResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        return body!.EventId;
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
    // Private request / response records (inline, not shared outside this file)
    // -------------------------------------------------------------------------

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

    private sealed record CreateOfferRequest(
        string Name,
        string Currency,
        DateTimeOffset? SaleStart,
        DateTimeOffset? SaleEnd);

    private sealed record CreateOfferResponse(Guid OfferId);

    private sealed record ConfigurePricingRequest(IReadOnlyList<PriceZoneRequestItem> PriceZones);

    private sealed record PriceZoneRequestItem(
        string Name,
        decimal Price,
        IReadOnlyList<Guid> SeatIds,
        IReadOnlyList<Guid> PoolIds);
}

/// <summary>Input record for <see cref="TicketingSeedHelpers.ConfigurePriceZonesAsync"/>.</summary>
internal sealed record PriceZoneItem(
    string Name,
    decimal Price,
    IReadOnlyList<Guid> SeatIds,
    IReadOnlyList<Guid> PoolIds);
