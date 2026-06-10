using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Ticketing.Fixtures;
using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.Authorization;

/// <summary>
/// Verifies that all Ticketing mutation endpoints enforce role-based authorization,
/// and that GET endpoints enforce authentication.
/// </summary>
[Collection(EventsTestCollectionFixture.Name)]
public sealed class TicketingAuthorizationTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;
    private readonly HttpClient _adminClient;

    public TicketingAuthorizationTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
        _adminClient = fixture.CreateAdminClient();
    }

    [Fact]
    public async Task CreateOffer_WhenCallerIsNotEventManager_Returns403()
    {
        // Set up a published event using an EventManager client so the event exists.
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);

        // Attempt to create an offer using an EventAdmin — role is not EventManager.
        CreateOfferRequest request = new(
            Name: $"Admin Offer {Guid.NewGuid()}",
            Currency: "USD",
            SaleStart: null,
            SaleEnd: null);

        HttpResponseMessage response = await _adminClient.PostAsJsonAsync($"/events/{eventId}/offers", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ConfigurePricing_WhenCallerIsNotEventManager_Returns403()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);

        // Resolve a valid seat ID for the request (we need at least one to avoid a 400).
        List<Guid> seatIds = await GetInventorySeatIdsAsync(eventId);

        ConfigurePricingRequest request = new(
            PriceZones: [new PriceZoneRequestItem("Zone A", 25.00m, [seatIds[0]], [])]);

        HttpResponseMessage response = await _adminClient.PutAsJsonAsync($"/offers/{offerId}/price-zones", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ActivateOffer_WhenCallerIsNotEventManager_Returns403()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);

        // The offer doesn't need to be ready for activation — authorization is checked first.
        HttpResponseMessage response = await _adminClient.PostAsync($"/offers/{offerId}/activate", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOffer_WhenUnauthenticated_Returns401()
    {
        HttpClient unauthenticated = _fixture.Client;

        HttpResponseMessage response = await unauthenticated.GetAsync($"/offers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOffers_WhenUnauthenticated_Returns401()
    {
        HttpClient unauthenticated = _fixture.Client;

        HttpResponseMessage response = await unauthenticated.GetAsync($"/events/{Guid.NewGuid()}/offers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInventoryStatus_WhenUnauthenticated_Returns401()
    {
        HttpClient unauthenticated = _fixture.Client;

        HttpResponseMessage response = await unauthenticated.GetAsync($"/events/{Guid.NewGuid()}/inventory");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<List<Guid>> GetInventorySeatIdsAsync(Guid eventId)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var reference = await db.PublishedEventReferences
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EventId == eventId);

        Assert.NotNull(reference);

        var inventory = await db.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.EventReferenceId == reference!.Id);

        Assert.NotNull(inventory);

        return inventory!.Seats.Select(s => s.Id.Value).ToList();
    }

    private sealed record CreateOfferRequest(
        string Name,
        string Currency,
        DateTimeOffset? SaleStart,
        DateTimeOffset? SaleEnd);

    private sealed record ConfigurePricingRequest(IReadOnlyList<PriceZoneRequestItem> PriceZones);

    private sealed record PriceZoneRequestItem(
        string Name,
        decimal Price,
        IReadOnlyList<Guid> SeatIds,
        IReadOnlyList<Guid> PoolIds);
}
