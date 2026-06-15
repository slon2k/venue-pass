using System.Net;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.IntegrationTests.Ticketing.Fixtures;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.Orders;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class GetOrderTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;
    private readonly HttpClient _customerClient;

    public GetOrderTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
        _customerClient = fixture.CreateAuthenticatedCustomerClient();
    }

    [Fact]
    public async Task GetOrder_WhenOrderExists_Returns200WithAllFields()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, List<Guid> poolIds) = await GetInventoryIdsAsync(eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(
            _managerClient, offerId,
            [new PriceZoneItem("Zone A", 75m, seatIds, poolIds)]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient, offerId, seatIds: [seatIds[0]]);

        Guid orderId = await TicketingSeedHelpers.CheckoutReservationAsync(
            _customerClient, reservationId, "Alice Smith", "alice@example.com");

        GetOrderSeedResponse order = await TicketingSeedHelpers.GetOrderAsync(_customerClient, orderId);

        Assert.Equal(orderId, order.OrderId);
        Assert.Equal(reservationId, order.ReservationId);
        Assert.Equal("Completed", order.Status);
        Assert.Equal("USD", order.Currency);
        Assert.Equal("Alice Smith", order.BuyerName);
        Assert.Equal("alice@example.com", order.BuyerEmail);
        Assert.Single(order.Items);
        Assert.Equal("Seat", order.Items[0].Type);
        Assert.Equal(seatIds[0], order.Items[0].InventorySeatId);
        Assert.Equal(1, order.Items[0].Quantity);
        Assert.Equal(75m, order.Items[0].UnitPrice);
        Assert.Equal(75m, order.Items[0].Total);
        Assert.Equal(75m, order.Total);
    }

    [Fact]
    public async Task GetOrder_WhenOrderHasMultipleItems_ReturnsAllItems()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, List<Guid> poolIds) = await GetInventoryIdsAsync(eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(
            _managerClient, offerId,
            [new PriceZoneItem("Zone A", 40m, seatIds, poolIds)]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient,
            offerId,
            seatIds: [seatIds[0]],
            gaPoolSelections: [new GaPoolSelectionItem(poolIds[0], 4)]);

        Guid orderId = await TicketingSeedHelpers.CheckoutReservationAsync(
            _customerClient, reservationId);

        GetOrderSeedResponse order = await TicketingSeedHelpers.GetOrderAsync(_customerClient, orderId);

        Assert.Equal(2, order.Items.Count);
        Assert.Equal(200m, order.Total); // 1 seat × 40 + 4 GA × 40

        var seatItem = order.Items.Single(i => i.Type == "Seat");
        Assert.Equal(seatIds[0], seatItem.InventorySeatId);

        var gaItem = order.Items.Single(i => i.Type == "GeneralAdmissionPool");
        Assert.Equal(poolIds[0], gaItem.GeneralAdmissionPoolId);
        Assert.Equal(4, gaItem.Quantity);
    }

    [Fact]
    public async Task GetOrder_UnknownOrderId_Returns404()
    {
        HttpResponseMessage response = await _customerClient.GetAsync($"/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<(List<Guid> SeatIds, List<Guid> PoolIds)> GetInventoryIdsAsync(Guid eventId)
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

        return (
            inventory!.Seats.Select(s => s.Id.Value).ToList(),
            inventory.Pools.Select(p => p.Id.Value).ToList());
    }
}
