using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Features.GetOffers;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Features.GetOffers;

public sealed class GetOffersHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public GetOffersHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Handle_WhenEventReferenceDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        await using var db = CreateDbContext();
        var handler = new GetOffersHandler(db);
        var unknownEventId = Guid.CreateVersion7();

        // Act
        var result = await handler.Handle(new GetOffersQuery(unknownEventId), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(GetOffersErrors.EventNotFound(unknownEventId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenEventReferenceExistsButNoInventory_ReturnsNotFoundError()
    {
        // Arrange
        await using var db = CreateDbContext();
        var eventId = Guid.CreateVersion7();
        db.PublishedEventReferences.Add(
            PublishedEventReference.Create(eventId, Guid.CreateVersion7(), DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var handler = new GetOffersHandler(db);

        // Act
        var result = await handler.Handle(new GetOffersQuery(eventId), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(GetOffersErrors.EventNotFound(eventId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenEventHasNoOffers_ReturnsEmptyList()
    {
        // Arrange
        await using var db = CreateDbContext();
        var (eventId, _) = CreateEventWithInventory(db);

        var handler = new GetOffersHandler(db);

        // Act
        var result = await handler.Handle(new GetOffersQuery(eventId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Offers);
    }

    [Fact]
    public async Task Handle_WhenEventHasOffers_ReturnsAllOffers()
    {
        // Arrange
        await using var db = CreateDbContext();
        var (eventId, inventory) = CreateEventWithInventory(db);

        var salesRange = CreateSalesRange();
        var offer1 = Offer.Create(inventory.Id, new OfferName("Standard"), salesRange, Currency.USD);
        var offer2 = Offer.Create(inventory.Id, new OfferName("VIP"), salesRange, Currency.EUR);
        db.Offers.AddRange(offer1, offer2);
        await db.SaveChangesAsync();

        var handler = new GetOffersHandler(db);

        // Act
        var result = await handler.Handle(new GetOffersQuery(eventId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Offers.Count);
        Assert.Contains(result.Value.Offers, o => o.Name == "Standard" && o.Currency == "USD");
        Assert.Contains(result.Value.Offers, o => o.Name == "VIP" && o.Currency == "EUR");
    }

    [Fact]
    public async Task Handle_WhenOfferHasPriceZones_ReturnsPriceZonesMapped()
    {
        // Arrange
        await using var db = CreateDbContext();
        var (eventId, inventory) = CreateEventWithInventory(db);

        var offer = Offer.Create(inventory.Id, new OfferName("Standard"), CreateSalesRange(), Currency.USD);
        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("General"),
            new Amount(30m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);

        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        var handler = new GetOffersHandler(db);

        // Act
        var result = await handler.Handle(new GetOffersQuery(eventId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Offers);
        Assert.Equal(offer.Id.Value, item.OfferId);
        Assert.Equal(inventory.Id.Value, item.InventoryId);
        Assert.Equal("Standard", item.Name);
        Assert.Equal("USD", item.Currency);
        Assert.Equal(OfferStatus.Draft.ToString(), item.Status);

        var zone = Assert.Single(item.PriceZones);
        Assert.Equal("General", zone.Name);
        Assert.Equal(30m, zone.Price);
        var seatId = Assert.Single(zone.SeatIds);
        Assert.Equal(inventory.Seats[0].Id.Value, seatId);
        Assert.Empty(zone.PoolIds);
    }

    [Fact]
    public async Task Handle_WhenMultipleInventoriesExist_ReturnsOnlyOffersForCorrectInventory()
    {
        // Arrange
        await using var db = CreateDbContext();
        var (eventId, inventory) = CreateEventWithInventory(db);
        var (_, otherInventory) = CreateEventWithInventory(db);

        var offer = Offer.Create(inventory.Id, new OfferName("Mine"), CreateSalesRange(), Currency.USD);
        var otherOffer = Offer.Create(otherInventory.Id, new OfferName("NotMine"), CreateSalesRange(), Currency.USD);
        db.Offers.AddRange(offer, otherOffer);
        await db.SaveChangesAsync();

        var handler = new GetOffersHandler(db);

        // Act
        var result = await handler.Handle(new GetOffersQuery(eventId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Offers);
        Assert.Equal("Mine", item.Name);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private TicketingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlite(_connection)
            .Options;

        var db = new TicketingDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static (Guid EventId, Inventory Inventory) CreateEventWithInventory(TicketingDbContext db)
    {
        var eventId = Guid.CreateVersion7();
        var reference = PublishedEventReference.Create(eventId, Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        db.PublishedEventReferences.Add(reference);
        db.SaveChanges();

        var manifest = new InventoryManifest(
            sections:
            [
                new InventorySectionInput("Main",
                [
                    new InventoryRowInput("A",
                    [
                        new InventorySeatInput(Guid.CreateVersion7(), "1"),
                        new InventorySeatInput(Guid.CreateVersion7(), "2")
                    ])
                ])
            ],
            generalAdmissionAreas: [new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Floor", 100)]);

        var inventory = Inventory.CreateFromManifest(reference.Id, manifest);
        db.Inventories.Add(inventory);
        db.SaveChanges();

        return (eventId, inventory);
    }

    private static DateTimeRange CreateSalesRange() => new(
        new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero));
}
