using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Ticketing.Fixtures;
using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.Offers;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class GetOfferTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;

    public GetOfferTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
    }

    [Fact]
    public async Task GetOffer_WhenOfferExists_Returns200WithCorrectShape()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        string offerName = $"Premium {Guid.NewGuid()}";
        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId, offerName, "EUR");

        HttpResponseMessage response = await _managerClient.GetAsync($"/offers/{offerId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetOfferResponse? body = await response.Content.ReadFromJsonAsync<GetOfferResponse>();
        Assert.NotNull(body);
        Assert.Equal(offerId, body!.OfferId);
        Assert.NotEqual(Guid.Empty, body.InventoryId);
        Assert.Equal(offerName, body.Name);
        Assert.Equal("EUR", body.Currency);
        Assert.Equal("Draft", body.Status);
        Assert.NotNull(body.PriceZones);
    }

    [Fact]
    public async Task GetOffer_WhenOfferDoesNotExist_Returns404()
    {
        HttpResponseMessage response = await _managerClient.GetAsync($"/offers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOffer_WhenUnauthenticated_Returns401()
    {
        HttpClient unauthenticated = _fixture.Client;

        HttpResponseMessage response = await unauthenticated.GetAsync($"/offers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOffers_WhenEventHasOffers_Returns200WithList()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);

        string offer1Name = $"Standard {Guid.NewGuid()}";
        string offer2Name = $"VIP {Guid.NewGuid()}";

        Guid offer1Id = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId, offer1Name, "USD");
        Guid offer2Id = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId, offer2Name, "USD");

        HttpResponseMessage response = await _managerClient.GetAsync($"/events/{eventId}/offers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetOffersResponse? body = await response.Content.ReadFromJsonAsync<GetOffersResponse>();
        Assert.NotNull(body);
        Assert.NotNull(body!.Offers);

        var offerIds = body.Offers.Select(o => o.OfferId).ToList();
        Assert.Contains(offer1Id, offerIds);
        Assert.Contains(offer2Id, offerIds);

        var firstOffer = body.Offers.First(o => o.OfferId == offer1Id);
        Assert.Equal(offer1Name, firstOffer.Name);
        Assert.Equal("USD", firstOffer.Currency);
        Assert.Equal("Draft", firstOffer.Status);
    }

    [Fact]
    public async Task GetOffers_WhenEventNotPublished_Returns404()
    {
        Guid unpublishedEventId = Guid.NewGuid();

        HttpResponseMessage response = await _managerClient.GetAsync($"/events/{unpublishedEventId}/offers");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private sealed record GetOffersResponse(IReadOnlyList<GetOffersItemResponse> Offers);

    private sealed record GetOffersItemResponse(
        Guid OfferId,
        Guid InventoryId,
        string Name,
        string Currency,
        string Status,
        DateTimeOffset? SaleStart,
        DateTimeOffset? SaleEnd,
        IReadOnlyList<GetOfferPriceZoneResponse> PriceZones);
}
