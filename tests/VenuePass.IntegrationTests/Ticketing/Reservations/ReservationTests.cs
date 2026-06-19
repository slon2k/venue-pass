using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.IntegrationTests.Ticketing.Fixtures;
using VenuePass.Modules.Ticketing.Infrastructure;
using VenuePass.Modules.Ticketing.Options;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.Reservations;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class ReservationsTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;
    private readonly HttpClient _customerClient;

    public ReservationsTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
        _customerClient = fixture.CreateAuthenticatedCustomerClient();
    }

    [Fact]
    public async Task ReservationFlow_WhenActiveOfferAndAvailableSeat_ReturnsCreatedReservation()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, _) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId, saleStart: DateTimeOffset.UtcNow.AddMinutes(-5), saleEnd: DateTimeOffset.UtcNow.AddHours(1));
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, [new PriceZoneItem("Zone A", 25m, seatIds, [])]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            "/reservations",
            new CreateReservationRequest(offerId, [seatIds[0]], []));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateReservationResponse? body = await response.Content.ReadFromJsonAsync<CreateReservationResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.ReservationId);
        Assert.Equal("Reserved", body.Status);
        Assert.Equal("USD", body.Currency);
        Assert.Single(body.Items);
        Assert.Equal(25m, body.Items[0].UnitPrice);
        Assert.Equal(25m, body.Total);
    }

    [Fact]
    public async Task ReservationFlow_WhenActiveOfferAndAvailableGaPool_ReturnsCreatedReservation()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (_, List<Guid> poolIds) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId, saleStart: DateTimeOffset.UtcNow.AddMinutes(-5), saleEnd: DateTimeOffset.UtcNow.AddHours(1));
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, [new PriceZoneItem("Zone A", 30m, [], poolIds)]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            "/reservations",
            new CreateReservationRequest(offerId, [], [new GaPoolSelectionItem(poolIds[0], 4)]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateReservationResponse? body = await response.Content.ReadFromJsonAsync<CreateReservationResponse>();
        Assert.NotNull(body);
        Assert.Single(body!.Items);
        Assert.Equal("GeneralAdmissionPool", body.Items[0].Type);
        Assert.Equal(4, body.Items[0].Quantity);
        Assert.Equal(120m, body.Total);
    }

    [Fact]
    public async Task ReservationRejection_WhenSeatUnavailable_ReturnsConflict()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, _) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, [new PriceZoneItem("Zone A", 25m, seatIds, [])]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(_customerClient, offerId, seatIds: [seatIds[0]]);

        HttpResponseMessage duplicate = await _customerClient.PostAsJsonAsync(
            "/reservations",
            new CreateReservationRequest(offerId, [seatIds[0]], []));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        HttpResponseMessage getReservation = await _customerClient.GetAsync($"/reservations/{reservationId}");
        Assert.Equal(HttpStatusCode.OK, getReservation.StatusCode);
    }

    [Fact]
    public async Task ReservationRejection_WhenOfferInactive_ReturnsConflict()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, _) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, [new PriceZoneItem("Zone A", 25m, seatIds, [])]);

        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            "/reservations",
            new CreateReservationRequest(offerId, [seatIds[0]], []));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ReservationRejection_WhenSaleWindowEnded_ReturnsConflict()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, _) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(
            _managerClient,
            eventId,
            saleStart: DateTimeOffset.UtcNow.AddHours(-2),
            saleEnd: DateTimeOffset.UtcNow.AddMinutes(-1));

        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, [new PriceZoneItem("Zone A", 25m, seatIds, [])]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            "/reservations",
            new CreateReservationRequest(offerId, [seatIds[0]], []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CancelReservation_WhenReserved_ReleasesInventory()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, List<Guid> poolIds) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, [new PriceZoneItem("Zone A", 25m, seatIds, poolIds)]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(_customerClient, offerId, seatIds: [seatIds[0]], gaPoolSelections: [new GaPoolSelectionItem(poolIds[0], 5)]);

        await TicketingSeedHelpers.CancelReservationAsync(_customerClient, reservationId);

        TicketingSeedHelpers.GetReservationSeedResponse reservation = await TicketingSeedHelpers.GetReservationAsync(_customerClient, reservationId);
        Assert.Equal("Cancelled", reservation.Status);

        GetInventoryStatusResponse inventory = await GetInventoryStatusAsync(eventId);
        Assert.Equal(2, inventory.AvailableSeats);
        Assert.Equal(300, inventory.Pools[0].AvailableCount);
    }

    [Fact]
    public async Task ExpireReservationWorker_WhenReservationIsOverdue_ReleasesInventory()
    {
        await using EventsApiFactory factory = _fixture.CreateFactory(
            enableOutboxDispatcher: true,
            configureTestServices: services =>
            {
                services.RemoveAll<Microsoft.Extensions.Options.IOptions<TicketingOptions>>();
                services.Configure<TicketingOptions>(options =>
                {
                    options.ReservationExpiryMinutes = 15;
                    options.ExpirationSweepIntervalSeconds = 1;
                    options.BatchSize = 20;
                });
            });

        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(factory);
        HttpClient managerClient = CreateManagerClient(factory);
        HttpClient customerClient = CreateCustomerClient(factory);

        (List<Guid> seatIds, _) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(managerClient, offerId, [new PriceZoneItem("Zone A", 25m, seatIds, [])]);
        await TicketingSeedHelpers.ActivateOfferAsync(managerClient, offerId);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(customerClient, offerId, seatIds: [seatIds[0]]);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [ticketing].[reservations]
                SET [expires_at] = {DateTimeOffset.UtcNow.AddSeconds(-5)}
                WHERE [id] = {reservationId}
                """);
        }

        await WaitUntilAsync(async () =>
        {
            TicketingSeedHelpers.GetReservationSeedResponse current = await TicketingSeedHelpers.GetReservationAsync(customerClient, reservationId);
            return current.Status == "Expired";
        }, timeout: TimeSpan.FromSeconds(10));

        GetInventoryStatusResponse inventory = await GetInventoryStatusAsync(factory, eventId, managerClient);
        Assert.Equal(2, inventory.AvailableSeats);
    }

    [Fact]
    public async Task CheckoutThenGetTicketAndOrder_WhenReservationCompletes_ReturnsIssuedTickets()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, List<Guid> poolIds) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, [new PriceZoneItem("Zone A", 20m, seatIds, poolIds)]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient,
            offerId,
            seatIds: [seatIds[0]],
            gaPoolSelections: [new GaPoolSelectionItem(poolIds[0], 2)]);

        Guid orderId = await TicketingSeedHelpers.CheckoutReservationAsync(_customerClient, reservationId, "Buyer", "buyer@example.com");
        GetOrderSeedResponse order = await TicketingSeedHelpers.GetOrderAsync(_customerClient, orderId);

        Assert.Equal("Completed", order.Status);
        Assert.Equal(3, order.Items.Sum(i => i.Quantity));
        Assert.Equal(3, order.Tickets.Count);
        Assert.Equal(order.Total, order.Items.Sum(i => i.Total));

        string firstCode = order.Tickets[0].Code;
        TicketingSeedHelpers.GetTicketSeedResponse ticket = await TicketingSeedHelpers.GetTicketAsync(_customerClient, firstCode);
        Assert.Equal(firstCode, ticket.Code);
        Assert.Equal("Issued", ticket.Status);
        Assert.NotEqual(Guid.Empty, ticket.TicketId);
    }

    [Fact]
    public async Task Concurrency_WhenDuplicateReservationsTargetSameSeat_OnlyOneSucceeds()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, _) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, [new PriceZoneItem("Zone A", 25m, seatIds, [])]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        HttpClient concurrentCustomer = _fixture.CreateAuthenticatedCustomerClient();

        Task<HttpResponseMessage> firstAttempt = _customerClient.PostAsJsonAsync(
            "/reservations",
            new CreateReservationRequest(offerId, [seatIds[0]], []));

        Task<HttpResponseMessage> secondAttempt = concurrentCustomer.PostAsJsonAsync(
            "/reservations",
            new CreateReservationRequest(offerId, [seatIds[0]], []));

        HttpResponseMessage[] responses = await Task.WhenAll(firstAttempt, secondAttempt);

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        GetInventoryStatusResponse inventory = await GetInventoryStatusAsync(eventId);
        Assert.Equal(1, inventory.AvailableSeats);
    }

    [Fact]
    public async Task EndToEnd_WhenEventPublishesAndCustomerBooks_ReturnsTicketsAndOrder()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, List<Guid> poolIds) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, [new PriceZoneItem("Zone A", 18m, seatIds, poolIds)]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(_customerClient, offerId, seatIds: [seatIds[0]], gaPoolSelections: [new GaPoolSelectionItem(poolIds[0], 1)]);
        Guid orderId = await TicketingSeedHelpers.CheckoutReservationAsync(_customerClient, reservationId);

        GetOrderSeedResponse order = await TicketingSeedHelpers.GetOrderAsync(_customerClient, orderId);
        TicketingSeedHelpers.GetTicketSeedResponse ticket = await TicketingSeedHelpers.GetTicketAsync(_customerClient, order.Tickets[0].Code);

        Assert.Equal(orderId, order.OrderId);
        Assert.Equal(reservationId, order.ReservationId);
        Assert.Equal(2, order.Tickets.Count);
        Assert.Equal(order.Tickets[0].Code, ticket.Code);
        Assert.Equal("Issued", ticket.Status);
    }

    [Fact]
    public async Task Authorization_WhenCustomerEndpointsAreUnauthenticated_ReturnsUnauthorized()
    {
        HttpClient unauthenticated = _fixture.Client;

        HttpResponseMessage createReservation = await unauthenticated.PostAsJsonAsync(
            "/reservations",
            new CreateReservationRequest(Guid.NewGuid(), [], []));

        HttpResponseMessage getReservation = await unauthenticated.GetAsync($"/reservations/{Guid.NewGuid()}");
        HttpResponseMessage cancelReservation = await unauthenticated.DeleteAsync($"/reservations/{Guid.NewGuid()}");
        HttpResponseMessage checkoutReservation = await unauthenticated.PostAsJsonAsync(
            $"/reservations/{Guid.NewGuid()}/checkout",
            new CheckoutReservationRequest("Buyer", "buyer@example.com"));
        HttpResponseMessage getOrder = await unauthenticated.GetAsync($"/orders/{Guid.NewGuid()}");
        HttpResponseMessage getTicket = await unauthenticated.GetAsync($"/tickets/{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Unauthorized, createReservation.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getReservation.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, cancelReservation.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, checkoutReservation.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getOrder.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getTicket.StatusCode);
    }

    [Fact]
    public async Task Authorization_WhenCustomerEndpointsAreAuthenticated_ReturnsNon403ForStandardCustomerRole()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, _) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(_managerClient, offerId, [new PriceZoneItem("Zone A", 25m, seatIds, [])]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        HttpResponseMessage createReservation = await _customerClient.PostAsJsonAsync(
            "/reservations",
            new CreateReservationRequest(offerId, [seatIds[0]], []));

        Assert.NotEqual(HttpStatusCode.Forbidden, createReservation.StatusCode);
    }

    private async Task<GetInventoryStatusResponse> GetInventoryStatusAsync(Guid eventId)
    {
        HttpResponseMessage response = await _managerClient.GetAsync($"/events/{eventId}/inventory");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetInventoryStatusResponse? body = await response.Content.ReadFromJsonAsync<GetInventoryStatusResponse>();
        Assert.NotNull(body);
        return body!;
    }

    private static async Task<GetInventoryStatusResponse> GetInventoryStatusAsync(
        EventsApiFactory factory,
        Guid eventId,
        HttpClient managerClient)
    {
        HttpResponseMessage response = await managerClient.GetAsync($"/events/{eventId}/inventory");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetInventoryStatusResponse? body = await response.Content.ReadFromJsonAsync<GetInventoryStatusResponse>();
        Assert.NotNull(body);
        return body!;
    }

    private static HttpClient CreateManagerClient(EventsApiFactory factory)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventManager");
        return client;
    }

    private static HttpClient CreateCustomerClient(EventsApiFactory factory)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        return client;
    }

    private static async Task<HttpResponseMessage> ExpireReservationAsync(IServiceProvider serviceProvider, Guid reservationId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [ticketing].[reservations]
            SET [expires_at] = {DateTimeOffset.UtcNow.AddSeconds(-1)}
            WHERE [id] = {reservationId}
            """);

        return new HttpResponseMessage(HttpStatusCode.NoContent);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan timeout)
    {
        DateTimeOffset start = DateTimeOffset.UtcNow;

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

    private sealed record CreateReservationRequest(
        Guid OfferId,
        IReadOnlyList<Guid> SeatIds,
        IReadOnlyList<GaPoolSelectionItem> GaPoolSelections);

    private sealed record CheckoutReservationRequest(string BuyerName, string BuyerEmail);

    private sealed record CreateReservationResponse(
        Guid ReservationId,
        string Status,
        DateTimeOffset ExpiresAt,
        string Currency,
        decimal Total,
        IReadOnlyList<CreateReservationItemResponse> Items);

    private sealed record CreateReservationItemResponse(
        Guid ReservationItemId,
        string Type,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        Guid PriceZoneId,
        int Quantity,
        decimal UnitPrice,
        decimal Total);

    private sealed record GetInventoryStatusResponse(
        Guid EventId,
        Guid InventoryId,
        int TotalSeats,
        int AvailableSeats,
        IReadOnlyList<SectionStatusResponse> Sections,
        IReadOnlyList<PoolStatusResponse> Pools);

    private sealed record SectionStatusResponse(string Name, int TotalSeats, int AvailableSeats);

    private sealed record PoolStatusResponse(string Name, int TotalCapacity, int AvailableCount);
}
