using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.IntegrationTests.Ticketing.Fixtures;
using VenuePass.Modules.Ticketing.Domain.Reservations;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.Orders;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class CheckoutReservationTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;
    private readonly HttpClient _customerClient;

    public CheckoutReservationTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
        _customerClient = fixture.CreateAuthenticatedCustomerClient();
    }

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Checkout_SeatReservation_Returns201WithCompletedOrder()
    {
        (Guid offerId, List<Guid> seatIds, _) = await SetupActiveOfferAsync();

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient, offerId, seatIds: [seatIds[0]]);

        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            $"/reservations/{reservationId}/checkout",
            new CheckoutRequest("Jane Doe", "jane@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CheckoutResponse? body = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.OrderId);
        Assert.Equal(reservationId, body.ReservationId);
        Assert.Equal("Completed", body.Status);
        Assert.Equal("USD", body.Currency);
        Assert.Equal("Jane Doe", body.BuyerName);
        Assert.Equal("jane@example.com", body.BuyerEmail);
        Assert.Single(body.Items);
        Assert.Equal("Seat", body.Items[0].Type);
        Assert.Equal(seatIds[0], body.Items[0].InventorySeatId);
        Assert.Equal(1, body.Items[0].Quantity);
        Assert.Single(body.Tickets);
        Assert.NotEqual(Guid.Empty, body.Tickets[0].TicketId);
        Assert.Equal(seatIds[0], body.Tickets[0].InventorySeatId);
        Assert.Null(body.Tickets[0].GeneralAdmissionPoolId);
        Assert.Equal(16, body.Tickets[0].Code.Length);
        Assert.True(body.Tickets[0].CreatedAt > DateTimeOffset.MinValue);
        Assert.True(body.Total > 0);
        Assert.Equal(body.Total, body.Items[0].Total);
    }

    [Fact]
    public async Task Checkout_GaPoolReservation_Returns201WithCompletedOrder()
    {
        (Guid offerId, _, List<Guid> poolIds) = await SetupActiveOfferAsync();

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient,
            offerId,
            gaPoolSelections: [new GaPoolSelectionItem(poolIds[0], 3)]);

        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            $"/reservations/{reservationId}/checkout",
            new CheckoutRequest("John Doe", "john@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CheckoutResponse? body = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(body);
        Assert.Equal("Completed", body!.Status);
        Assert.Single(body.Items);
        Assert.Equal("GeneralAdmissionPool", body.Items[0].Type);
        Assert.Equal(poolIds[0], body.Items[0].GeneralAdmissionPoolId);
        Assert.Equal(3, body.Items[0].Quantity);
        Assert.Equal(3, body.Tickets.Count);
        Assert.All(body.Tickets, t => Assert.Equal(poolIds[0], t.GeneralAdmissionPoolId));
        Assert.All(body.Tickets, t => Assert.Null(t.InventorySeatId));
        Assert.Equal(body.Tickets.Count, body.Tickets.Select(t => t.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(body.Items[0].UnitPrice * 3, body.Total);
    }

    [Fact]
    public async Task Checkout_MixedReservation_Returns201WithCorrectTotal()
    {
        (Guid offerId, List<Guid> seatIds, List<Guid> poolIds) = await SetupActiveOfferAsync();

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient,
            offerId,
            seatIds: [seatIds[0]],
            gaPoolSelections: [new GaPoolSelectionItem(poolIds[0], 2)]);

        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            $"/reservations/{reservationId}/checkout",
            new CheckoutRequest("Mixed Buyer", "mixed@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CheckoutResponse? body = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Items.Count);
        Assert.Equal(3, body.Tickets.Count);

        decimal expectedTotal = body.Items.Sum(i => i.Total);
        Assert.Equal(expectedTotal, body.Total);
    }

    // -------------------------------------------------------------------------
    // Inventory mutation after checkout
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Checkout_SeatReservation_SeatBecomeSold()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, List<Guid> poolIds) = await GetInventoryIdsAsync(eventId);

        Guid offerId = await SetupOfferAsync(eventId, seatIds, poolIds, price: 25m);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient, offerId, seatIds: [seatIds[0]]);

        await TicketingSeedHelpers.CheckoutReservationAsync(_customerClient, reservationId);

        // Verify inventory: reserved seat must now be sold
        HttpResponseMessage invResponse = await _managerClient.GetAsync($"/events/{eventId}/inventory");
        Assert.Equal(HttpStatusCode.OK, invResponse.StatusCode);

        InventoryStatusResponse? inv = await invResponse.Content.ReadFromJsonAsync<InventoryStatusResponse>();
        Assert.NotNull(inv);

        // 2 seats total; 1 sold → 1 available (not 2)
        Assert.Equal(1, inv!.AvailableSeats);
        Assert.Equal(2, inv.TotalSeats);
    }

    [Fact]
    public async Task Checkout_GaPoolReservation_PoolAvailableCountUnchangedAfterCheckout()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, List<Guid> poolIds) = await GetInventoryIdsAsync(eventId);

        Guid offerId = await SetupOfferAsync(eventId, seatIds, poolIds, price: 15m);

        // Reserve 5 GA tickets
        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient,
            offerId,
            gaPoolSelections: [new GaPoolSelectionItem(poolIds[0], 5)]);

        // Inventory should show 295 available (300 - 5 reserved)
        InventoryStatusResponse? before = await GetInventoryStatusAsync(eventId);
        Assert.Equal(295, before!.Pools[0].AvailableCount);

        await TicketingSeedHelpers.CheckoutReservationAsync(_customerClient, reservationId);

        // After checkout, GA count should still be 295 (not restored)
        InventoryStatusResponse? after = await GetInventoryStatusAsync(eventId);
        Assert.Equal(295, after!.Pools[0].AvailableCount);
    }

    [Fact]
    public async Task Checkout_GaPoolReservation_PoolSoldCountIncrementedAfterCheckout()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, List<Guid> poolIds) = await GetInventoryIdsAsync(eventId);

        Guid offerId = await SetupOfferAsync(eventId, seatIds, poolIds, price: 20m);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient,
            offerId,
            gaPoolSelections: [new GaPoolSelectionItem(poolIds[0], 7)]);

        await TicketingSeedHelpers.CheckoutReservationAsync(_customerClient, reservationId);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var reference = await db.PublishedEventReferences
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EventId == eventId);

        var inventory = await db.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.EventReferenceId == reference!.Id);

        var pool = inventory!.Pools.Single(p => p.Id.Value == poolIds[0]);
        Assert.Equal(7, pool.SoldCount);
    }

    // -------------------------------------------------------------------------
    // Idempotency
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Checkout_AlreadyCompleted_Returns200WithSameOrder()
    {
        (Guid offerId, List<Guid> seatIds, _) = await SetupActiveOfferAsync();

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient, offerId, seatIds: [seatIds[0]]);

        // First checkout → 201
        HttpResponseMessage first = await _customerClient.PostAsJsonAsync(
            $"/reservations/{reservationId}/checkout",
            new CheckoutRequest("Buyer", "buyer@example.com"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        CheckoutResponse? firstBody = await first.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(firstBody);

        // Second checkout → 200 with the same order
        HttpResponseMessage second = await _customerClient.PostAsJsonAsync(
            $"/reservations/{reservationId}/checkout",
            new CheckoutRequest("Buyer", "buyer@example.com"));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        CheckoutResponse? secondBody = await second.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(secondBody);
        Assert.Equal(firstBody!.OrderId, secondBody!.OrderId);
        Assert.Equal(
            firstBody.Tickets.Select(t => t.Code).OrderBy(c => c),
            secondBody!.Tickets.Select(t => t.Code).OrderBy(c => c));
    }

    // -------------------------------------------------------------------------
    // Rejection cases
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Checkout_CancelledReservation_Returns409()
    {
        (Guid offerId, List<Guid> seatIds, _) = await SetupActiveOfferAsync();

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient, offerId, seatIds: [seatIds[0]]);

        // Cancel the reservation first
        HttpResponseMessage cancelResponse = await _customerClient.DeleteAsync($"/reservations/{reservationId}");
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            $"/reservations/{reservationId}/checkout",
            new CheckoutRequest("Buyer", "buyer@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_ExpiredReservation_Returns409()
    {
        (Guid offerId, List<Guid> seatIds, _) = await SetupActiveOfferAsync();

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient, offerId, seatIds: [seatIds[0]]);

        // Force the reservation into Expired status via direct DB update
        await SetReservationStatusAsync(reservationId, ReservationStatus.Expired);

        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            $"/reservations/{reservationId}/checkout",
            new CheckoutRequest("Buyer", "buyer@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_ReservedPastExpiry_Returns409()
    {
        (Guid offerId, List<Guid> seatIds, _) = await SetupActiveOfferAsync();

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient, offerId, seatIds: [seatIds[0]]);

        // Push ExpiresAt into the past while keeping status as Reserved
        await SetReservationExpiryAsync(reservationId, DateTimeOffset.UtcNow.AddMinutes(-30));

        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            $"/reservations/{reservationId}/checkout",
            new CheckoutRequest("Buyer", "buyer@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_UnknownReservation_Returns404()
    {
        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            $"/reservations/{Guid.NewGuid()}/checkout",
            new CheckoutRequest("Buyer", "buyer@example.com"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_InvalidRequest_Returns400()
    {
        HttpResponseMessage response = await _customerClient.PostAsJsonAsync(
            $"/reservations/{Guid.NewGuid()}/checkout",
            new CheckoutRequest(BuyerName: "", BuyerEmail: "not-an-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<(Guid OfferId, List<Guid> SeatIds, List<Guid> PoolIds)> SetupActiveOfferAsync(
        decimal price = 50m)
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, List<Guid> poolIds) = await GetInventoryIdsAsync(eventId);

        Guid offerId = await SetupOfferAsync(eventId, seatIds, poolIds, price);

        return (offerId, seatIds, poolIds);
    }

    private async Task<Guid> SetupOfferAsync(
        Guid eventId,
        List<Guid> seatIds,
        List<Guid> poolIds,
        decimal price)
    {
        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);

        await TicketingSeedHelpers.ConfigurePriceZonesAsync(
            _managerClient,
            offerId,
            [new PriceZoneItem("Zone A", price, seatIds, poolIds)]);

        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        return offerId;
    }

    private Task<(List<Guid> SeatIds, List<Guid> PoolIds)> GetInventoryIdsAsync(Guid eventId) =>
        TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

    private async Task SetReservationStatusAsync(Guid reservationId, ReservationStatus status)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var id = new ReservationId(reservationId);
        await db.Reservations
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, status));
    }

    private async Task SetReservationExpiryAsync(Guid reservationId, DateTimeOffset expiresAt)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var id = new ReservationId(reservationId);
        await db.Reservations
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ExpiresAt, expiresAt));
    }

    private async Task<InventoryStatusResponse?> GetInventoryStatusAsync(Guid eventId)
    {
        HttpResponseMessage response = await _managerClient.GetAsync($"/events/{eventId}/inventory");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<InventoryStatusResponse>();
    }

    // -------------------------------------------------------------------------
    // Private request / response records
    // -------------------------------------------------------------------------

    private sealed record CheckoutRequest(string BuyerName, string BuyerEmail);

    private sealed record CheckoutResponse(
        Guid OrderId,
        Guid ReservationId,
        string Status,
        string Currency,
        decimal Total,
        string BuyerName,
        string BuyerEmail,
        IReadOnlyList<CheckoutItemResponse> Items,
        IReadOnlyList<CheckoutTicketResponse> Tickets);

    private sealed record CheckoutItemResponse(
        Guid OrderItemId,
        string Type,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        Guid PriceZoneId,
        int Quantity,
        decimal UnitPrice,
        decimal Total);

    private sealed record CheckoutTicketResponse(
        Guid TicketId,
        string Code,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        DateTimeOffset CreatedAt);

    private sealed record InventoryStatusResponse(
        Guid EventId,
        Guid InventoryId,
        int TotalSeats,
        int AvailableSeats,
        IReadOnlyList<SectionStatusResponse> Sections,
        IReadOnlyList<PoolStatusResponse> Pools);

    private sealed record SectionStatusResponse(
        string Name,
        int TotalSeats,
        int AvailableSeats);

    private sealed record PoolStatusResponse(
        string Name,
        int TotalCapacity,
        int AvailableCount);
}
