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

namespace VenuePass.IntegrationTests.Ticketing.Fixtures;

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

        return await PublishEventAndSyncInventoryAsync(factory);
    }

    /// <summary>
    /// Creates a venue, manifest template, event, publishes it, and waits for
    /// the outbox dispatcher to process the message so the Ticketing module's
    /// EventPublishedHandler runs and inventory is created. Returns the eventId.
    /// </summary>
    public static async Task<Guid> PublishEventAndSyncInventoryAsync(EventsApiFactory factory)
    {
        // Use clients from the provided factory so callers can customize the DI container
        // (for example, to shorten the expiration sweep interval in background-worker tests).
        using HttpClient adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventAdmin");

        using HttpClient factoryManagerClient = factory.CreateClient();
        factoryManagerClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
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
        string currency = "USD",
        DateTimeOffset? saleStart = null,
        DateTimeOffset? saleEnd = null)
    {
        CreateOfferRequest request = new(
            Name: name,
            Currency: currency,
            SaleStart: saleStart,
            SaleEnd: saleEnd);

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

    /// <summary>POST /reservations — returns reservationId.</summary>
    public static async Task<Guid> CreateReservationAsync(
        HttpClient client,
        Guid offerId,
        IReadOnlyList<Guid>? seatIds = null,
        IReadOnlyList<GaPoolSelectionItem>? gaPoolSelections = null)
    {
        var request = new CreateReservationRequest(
            OfferId: offerId,
            SeatIds: seatIds ?? [],
            GaPoolSelections: gaPoolSelections?.Select(s => new GaPoolSelectionRequestItem(s.PoolId, s.Quantity)).ToList() ?? []);

        HttpResponseMessage response = await client.PostAsJsonAsync("/reservations", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateReservationResponse? body = await response.Content.ReadFromJsonAsync<CreateReservationResponse>();
        Assert.NotNull(body);

        return body!.ReservationId;
    }

    /// <summary>POST /reservations/{id}/checkout — returns orderId. Asserts 201 Created.</summary>
    public static async Task<Guid> CheckoutReservationAsync(
        HttpClient client,
        Guid reservationId,
        string buyerName = "Test Buyer",
        string buyerEmail = "buyer@example.com")
    {
        var request = new CheckoutReservationRequest(BuyerName: buyerName, BuyerEmail: buyerEmail);

        HttpResponseMessage response = await client.PostAsJsonAsync($"/reservations/{reservationId}/checkout", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CheckoutReservationResponse? body = await response.Content.ReadFromJsonAsync<CheckoutReservationResponse>();
        Assert.NotNull(body);

        return body!.OrderId;
    }

    /// <summary>GET /reservations/{id} — returns the response body. Asserts 200 OK.</summary>
    public static async Task<GetReservationSeedResponse> GetReservationAsync(HttpClient client, Guid reservationId)
    {
        HttpResponseMessage response = await client.GetAsync($"/reservations/{reservationId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetReservationSeedResponse? body = await response.Content.ReadFromJsonAsync<GetReservationSeedResponse>();
        Assert.NotNull(body);

        return body!;
    }

    /// <summary>DELETE /reservations/{id} — asserts 204 No Content.</summary>
    public static async Task CancelReservationAsync(HttpClient client, Guid reservationId)
    {
        HttpResponseMessage response = await client.DeleteAsync($"/reservations/{reservationId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>GET /tickets/{ticketCode} — returns the response body. Asserts 200 OK.</summary>
    public static async Task<GetTicketSeedResponse> GetTicketAsync(HttpClient client, string ticketCode)
    {
        HttpResponseMessage response = await client.GetAsync($"/tickets/{ticketCode}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetTicketSeedResponse? body = await response.Content.ReadFromJsonAsync<GetTicketSeedResponse>();
        Assert.NotNull(body);

        return body!;
    }

    /// <summary>GET /orders/{orderId} — returns the response body. Asserts 200 OK.</summary>
    public static async Task<GetOrderSeedResponse> GetOrderAsync(HttpClient client, Guid orderId)
    {
        HttpResponseMessage response = await client.GetAsync($"/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetOrderSeedResponse? body = await response.Content.ReadFromJsonAsync<GetOrderSeedResponse>();
        Assert.NotNull(body);

        return body!;
    }

    /// <summary>Loads seat and pool IDs for an event's inventory directly from the database.</summary>
    public static async Task<(List<Guid> SeatIds, List<Guid> PoolIds)> GetInventoryIdsAsync(
        EventsIntegrationTestFixture fixture, Guid eventId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var reference = await db.PublishedEventReferences
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EventId == eventId);

        Assert.NotNull(reference);

        var inventory = await db.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.EventReferenceId == reference!.Id);

        Assert.NotNull(inventory);

        return (
            inventory!.Seats.Select(s => s.Id.Value).ToList(),
            inventory.Pools.Select(p => p.Id.Value).ToList());
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

    private sealed record CreateReservationRequest(
        Guid OfferId,
        IReadOnlyList<Guid> SeatIds,
        IReadOnlyList<GaPoolSelectionRequestItem> GaPoolSelections);

    private sealed record GaPoolSelectionRequestItem(Guid PoolId, int Quantity);

    private sealed record CreateReservationResponse(Guid ReservationId);

    private sealed record CheckoutReservationRequest(string BuyerName, string BuyerEmail);

    private sealed record CheckoutReservationResponse(Guid OrderId);

    internal sealed record GetReservationSeedResponse(
        Guid ReservationId,
        Guid OfferId,
        Guid InventoryId,
        string Status,
        DateTimeOffset ExpiresAt,
        string Currency,
        decimal Total,
        IReadOnlyList<GetReservationItemSeedResponse> Items);

    internal sealed record GetReservationItemSeedResponse(
        Guid ReservationItemId,
        string Type,
        Guid PriceZoneId,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        decimal UnitPrice,
        int Quantity,
        decimal Total);

    internal sealed record GetTicketSeedResponse(
        Guid TicketId,
        string Code,
        string Status,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        DateTimeOffset CreatedAt);
}

/// <summary>Input record for <see cref="TicketingSeedHelpers.ConfigurePriceZonesAsync"/>.</summary>
internal sealed record PriceZoneItem(
    string Name,
    decimal Price,
    IReadOnlyList<Guid> SeatIds,
    IReadOnlyList<Guid> PoolIds);

/// <summary>Input record for <see cref="TicketingSeedHelpers.CreateReservationAsync"/>.</summary>
internal sealed record GaPoolSelectionItem(Guid PoolId, int Quantity);

/// <summary>Response returned by <see cref="TicketingSeedHelpers.GetOrderAsync"/>.</summary>
internal sealed record GetOrderSeedResponse(
    Guid OrderId,
    Guid ReservationId,
    string Status,
    string Currency,
    decimal Total,
    string BuyerName,
    string BuyerEmail,
    DateTimeOffset CreatedAt,
    IReadOnlyList<GetOrderItemSeedResponse> Items,
    IReadOnlyList<GetOrderTicketSeedResponse> Tickets);

internal sealed record GetOrderItemSeedResponse(
    Guid OrderItemId,
    string Type,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    Guid PriceZoneId,
    int Quantity,
    decimal UnitPrice,
    decimal Total);

internal sealed record GetOrderTicketSeedResponse(
    Guid TicketId,
    string Code,
    string Status,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    DateTimeOffset CreatedAt);
