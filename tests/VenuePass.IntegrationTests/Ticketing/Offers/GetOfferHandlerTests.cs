using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Features.GetOffer;
using VenuePass.Modules.Ticketing.Infrastructure;
using InventoryDomain = VenuePass.Modules.Ticketing.Domain.Inventories;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.Offers;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class GetOfferHandlerTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public GetOfferHandlerTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_WhenOfferDoesNotExist_ReturnsNotFoundError()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var handler = new GetOfferHandler(db);
        var missingId = Guid.CreateVersion7();

        var result = await handler.Handle(new GetOfferQuery(missingId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(GetOfferErrors.OfferNotFound(missingId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenOfferExists_ReturnsOfferWithCorrectScalars()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var inventory = CreateAndSaveInventory(db);
        var offer = CreateAndSaveOffer(db, inventory.Id);

        var handler = new GetOfferHandler(db);

        var result = await handler.Handle(new GetOfferQuery(offer.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(offer.Id.Value, dto.OfferId);
        Assert.Equal(offer.InventoryId.Value, dto.InventoryId);
        Assert.Equal(offer.Name.Value, dto.Name);
        Assert.Equal(offer.Currency.Value, dto.Currency);
        Assert.Equal(offer.Status.ToString(), dto.Status);
        Assert.Equal(offer.SalesRange.Start, dto.SaleStart);
        Assert.Equal(offer.SalesRange.End, dto.SaleEnd);
    }

    [Fact]
    public async Task Handle_WhenOfferHasPriceZoneWithSeat_ReturnsPriceZoneWithSeatIds()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var inventory = CreateAndSaveInventory(db);
        var offer = CreateAndSaveOffer(db, inventory.Id);

        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("VIP"),
            new Amount(150m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);

        await db.SaveChangesAsync();

        var handler = new GetOfferHandler(db);

        var result = await handler.Handle(new GetOfferQuery(offer.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var zone = Assert.Single(result.Value.PriceZones);
        Assert.Equal("VIP", zone.Name);
        Assert.Equal(150m, zone.Price);
        var seatId = Assert.Single(zone.SeatIds);
        Assert.Equal(inventory.Seats[0].Id.Value, seatId);
        Assert.Empty(zone.PoolIds);
    }

    [Fact]
    public async Task Handle_WhenOfferHasPriceZoneWithPool_ReturnsPriceZoneWithPoolIds()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var inventory = CreateAndSaveInventory(db);
        var offer = CreateAndSaveOffer(db, inventory.Id);

        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("Floor"),
            new Amount(50m),
            [],
            [new PriceZoneGeneralAdmissionPoolItemInput(inventory.Pools[0].Id)]);

        await db.SaveChangesAsync();

        var handler = new GetOfferHandler(db);

        var result = await handler.Handle(new GetOfferQuery(offer.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var zone = Assert.Single(result.Value.PriceZones);
        Assert.Empty(zone.SeatIds);
        var poolId = Assert.Single(zone.PoolIds);
        Assert.Equal(inventory.Pools[0].Id.Value, poolId);
    }

    [Fact]
    public async Task Handle_WhenOfferHasNoPriceZones_ReturnsEmptyPriceZonesList()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var inventory = CreateAndSaveInventory(db);
        var offer = CreateAndSaveOffer(db, inventory.Id);

        var handler = new GetOfferHandler(db);

        var result = await handler.Handle(new GetOfferQuery(offer.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.PriceZones);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static InventoryDomain.Inventory CreateAndSaveInventory(TicketingDbContext db)
    {
        var reference = PublishedEventReference.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow);
        db.PublishedEventReferences.Add(reference);
        db.SaveChanges();

        var manifest = new InventoryDomain.InventoryManifest(
            sections:
            [
                new InventoryDomain.InventorySectionInput("Main",
                [
                    new InventoryDomain.InventoryRowInput("A",
                    [
                        new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "1"),
                        new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "2")
                    ])
                ])
            ],
            generalAdmissionAreas: [new InventoryDomain.InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Floor", 100)]);

        var inventory = InventoryDomain.Inventory.CreateFromManifest(reference.Id, manifest);
        db.Inventories.Add(inventory);
        db.SaveChanges();
        return inventory;
    }

    private static Offer CreateAndSaveOffer(TicketingDbContext db, InventoryDomain.InventoryId inventoryId)
    {
        var salesRange = new DateTimeRange(
            new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero));

        var offer = Offer.Create(inventoryId, new OfferName("Standard"), salesRange, Currency.USD);
        db.Offers.Add(offer);
        db.SaveChanges();
        return offer;
    }
}

