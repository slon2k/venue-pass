using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Ticketing.Fixtures;
using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.Offers;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class ConfigurePricingTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;

    public ConfigurePricingTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
    }

    [Fact]
    public async Task ConfigurePricing_WhenDraftOfferAndValidSeats_Returns204AndPersistsZones()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);

        // Resolve seat IDs from the inventory so we pass valid targets.
        List<Guid> seatIds = await GetInventorySeatIdsAsync(eventId);
        Assert.NotEmpty(seatIds);

        IReadOnlyList<PriceZoneItem> zones =
        [
            new PriceZoneItem("Floor A", 49.99m, [seatIds[0]], [])
        ];

        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, zones);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var offer = await db.Offers
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == new OfferId(offerId));

        Assert.NotNull(offer);
        Assert.Single(offer!.PriceZones);
        Assert.Equal("Floor A", offer.PriceZones[0].Name.Value);
        Assert.Equal(49.99m, offer.PriceZones[0].Price.Value);
        Assert.Contains(offer.PriceZones[0].InventorySeatItems, i => i.InventorySeatId.Value == seatIds[0]);
    }

    [Fact]
    public async Task ConfigurePricing_WhenNonExistentSeatId_Returns400()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);

        IReadOnlyList<PriceZoneItem> zones =
        [
            new PriceZoneItem("Ghost Zone", 25.00m, [Guid.NewGuid()], [])
        ];

        ConfigurePricingRequest request = new(
            PriceZones: [.. zones.Select(z => new PriceZoneRequestItem(z.Name, z.Price, [.. z.SeatIds], [.. z.PoolIds]))]);

        HttpResponseMessage response = await _managerClient.PutAsJsonAsync($"/offers/{offerId}/price-zones", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfigurePricing_WhenOfferIsActive_Returns400()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);

        List<Guid> seatIds = await GetInventorySeatIdsAsync(eventId);

        // Configure zones so the offer can be activated.
        IReadOnlyList<PriceZoneItem> zones =
        [
            new PriceZoneItem("Floor A", 49.99m, [seatIds[0]], [])
        ];
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, zones);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        // Attempt to reconfigure pricing on an Active offer — should fail.
        IReadOnlyList<PriceZoneItem> reconfigureZones =
        [
            new PriceZoneItem("Floor A Updated", 59.99m, [seatIds[0]], [])
        ];

        ConfigurePricingRequest request = new(
            PriceZones: [.. reconfigureZones.Select(z => new PriceZoneRequestItem(z.Name, z.Price, [.. z.SeatIds], [.. z.PoolIds]))]);

        HttpResponseMessage response = await _managerClient.PutAsJsonAsync($"/offers/{offerId}/price-zones", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfigurePricing_WhenUnauthenticated_Returns401()
    {
        HttpClient unauthenticated = _fixture.Client;

        ConfigurePricingRequest request = new(
            PriceZones: [new PriceZoneRequestItem("Zone A", 25.00m, [Guid.NewGuid()], [])]);

        HttpResponseMessage response = await unauthenticated.PutAsJsonAsync($"/offers/{Guid.NewGuid()}/price-zones", request);

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

    private sealed record ConfigurePricingRequest(IReadOnlyList<PriceZoneRequestItem> PriceZones);

    private sealed record PriceZoneRequestItem(
        string Name,
        decimal Price,
        IReadOnlyList<Guid> SeatIds,
        IReadOnlyList<Guid> PoolIds);
}
