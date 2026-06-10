using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.Modules.Events.IntegrationTests.Features.Ticketing.Fixtures;
using VenuePass.Modules.Events.IntegrationTests.Infrastructure;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Features.Ticketing.Offers;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class ActivateOfferTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;

    public ActivateOfferTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
    }

    [Fact]
    public async Task ActivateOffer_WhenPriceZonesConfigured_Returns204AndStatusIsActive()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);

        List<Guid> seatIds = await GetInventorySeatIdsAsync(eventId);
        Assert.NotEmpty(seatIds);

        IReadOnlyList<PriceZoneItem> zones =
        [
            new PriceZoneItem("Floor A", 49.99m, [seatIds[0]], [])
        ];
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, zones);

        HttpResponseMessage activateResponse = await _managerClient.PostAsync($"/offers/{offerId}/activate", null);
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);

        // Verify via GET that status is now Active.
        HttpResponseMessage getResponse = await _managerClient.GetAsync($"/offers/{offerId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        GetOfferResponse? body = await getResponse.Content.ReadFromJsonAsync<GetOfferResponse>();
        Assert.NotNull(body);
        Assert.Equal("Active", body!.Status);
    }

    [Fact]
    public async Task ActivateOffer_WhenNoPriceZones_Returns400()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);

        // Do not configure any price zones — activate should be rejected.
        HttpResponseMessage response = await _managerClient.PostAsync($"/offers/{offerId}/activate", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ActivateOffer_WhenAlreadyActive_Returns400()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);

        List<Guid> seatIds = await GetInventorySeatIdsAsync(eventId);

        IReadOnlyList<PriceZoneItem> zones =
        [
            new PriceZoneItem("Floor A", 49.99m, [seatIds[0]], [])
        ];
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, zones);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        // Second activation attempt on an already-active offer.
        HttpResponseMessage secondActivate = await _managerClient.PostAsync($"/offers/{offerId}/activate", null);

        Assert.Equal(HttpStatusCode.BadRequest, secondActivate.StatusCode);
    }

    [Fact]
    public async Task ActivateOffer_WhenUnauthenticated_Returns401()
    {
        HttpClient unauthenticated = _fixture.Client;

        HttpResponseMessage response = await unauthenticated.PostAsync($"/offers/{Guid.NewGuid()}/activate", null);

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

    private sealed record GetOfferResponse(
        Guid OfferId,
        Guid InventoryId,
        string Name,
        string Currency,
        string Status,
        DateTimeOffset? SaleStart,
        DateTimeOffset? SaleEnd,
        IReadOnlyList<GetOfferPriceZoneResponse> PriceZones);

    private sealed record GetOfferPriceZoneResponse(
        Guid PriceZoneId,
        string Name,
        decimal Price,
        IReadOnlyList<Guid> SeatIds,
        IReadOnlyList<Guid> PoolIds);
}
