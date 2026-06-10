using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.Modules.Events.IntegrationTests.Features.Ticketing.Fixtures;
using VenuePass.Modules.Events.IntegrationTests.Infrastructure;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Features.Ticketing.Offers;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class CreateOfferTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;

    public CreateOfferTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
    }

    [Fact]
    public async Task CreateOffer_WhenEventIsPublished_Returns201AndCreatesDraftOffer()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);

        CreateOfferRequest request = new(
            Name: $"Standard {Guid.NewGuid()}",
            Currency: "USD",
            SaleStart: null,
            SaleEnd: null);

        HttpResponseMessage response = await _managerClient.PostAsJsonAsync($"/events/{eventId}/offers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        CreateOfferResponse? body = await response.Content.ReadFromJsonAsync<CreateOfferResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.OfferId);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var offer = await db.Offers
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == new OfferId(body.OfferId));

        Assert.NotNull(offer);
        Assert.Equal(OfferStatus.Draft, offer!.Status);
        Assert.Equal(request.Name, offer.Name.Value);
        Assert.Equal(request.Currency, offer.Currency.Value);
    }

    [Fact]
    public async Task CreateOffer_WhenEventNotPublished_Returns404()
    {
        Guid nonPublishedEventId = Guid.NewGuid();

        CreateOfferRequest request = new(
            Name: $"Standard {Guid.NewGuid()}",
            Currency: "USD",
            SaleStart: null,
            SaleEnd: null);

        HttpResponseMessage response = await _managerClient.PostAsJsonAsync($"/events/{nonPublishedEventId}/offers", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_WhenNameIsEmpty_Returns400()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);

        CreateOfferRequest request = new(
            Name: "",
            Currency: "USD",
            SaleStart: null,
            SaleEnd: null);

        HttpResponseMessage response = await _managerClient.PostAsJsonAsync($"/events/{eventId}/offers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_WhenCurrencyInvalid_Returns400()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);

        CreateOfferRequest request = new(
            Name: $"Standard {Guid.NewGuid()}",
            Currency: "INVALID_CURRENCY_CODE",
            SaleStart: null,
            SaleEnd: null);

        HttpResponseMessage response = await _managerClient.PostAsJsonAsync($"/events/{eventId}/offers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_WhenUnauthenticated_Returns401()
    {
        HttpClient unauthenticated = _fixture.Client;

        CreateOfferRequest request = new(
            Name: $"Standard {Guid.NewGuid()}",
            Currency: "USD",
            SaleStart: null,
            SaleEnd: null);

        HttpResponseMessage response = await unauthenticated.PostAsJsonAsync($"/events/{Guid.NewGuid()}/offers", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record CreateOfferRequest(
        string Name,
        string Currency,
        DateTimeOffset? SaleStart,
        DateTimeOffset? SaleEnd);

    private sealed record CreateOfferResponse(Guid OfferId);
}
