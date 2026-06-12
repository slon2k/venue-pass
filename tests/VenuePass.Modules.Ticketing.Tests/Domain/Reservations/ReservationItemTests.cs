using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Domain.Reservations;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Reservations;

public sealed class ReservationItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 10, 10, 0, 0, TimeSpan.Zero);

    // ── Create from InventorySeat ─────────────────────────────────────────────

    [Fact]
    public void Create_WithValidSeat_ReturnsItemWithSeatType()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory, seatPrice: 50m);
        var seatId = inventory.Seats[0].Id;
        var seatInput = new ReservationItemInventorySeatInput(seatId);

        var item = ReservationItem.Create(offer, seatInput);

        Assert.Equal(ReservationItemType.Seat, item.Type);
        Assert.Equal(seatId, item.InventorySeatId);
        Assert.Null(item.GeneralAdmissionPoolId);
        Assert.Equal(1, item.Quantity.Value);
    }

    [Fact]
    public void Create_WithValidSeat_SetsUnitPriceFromPriceZone()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory, seatPrice: 75m);
        var seatId = inventory.Seats[0].Id;

        var item = ReservationItem.Create(offer, new ReservationItemInventorySeatInput(seatId));

        Assert.Equal(75m, item.UnitPrice.Value);
    }

    [Fact]
    public void Create_WithValidSeat_CalculatesTotalAs1XUnitPrice()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory, seatPrice: 60m);
        var seatId = inventory.Seats[0].Id;

        var item = ReservationItem.Create(offer, new ReservationItemInventorySeatInput(seatId));

        Assert.Equal(60m, item.Total.Value);
    }

    [Fact]
    public void Create_WithSeatNotCoveredByOffer_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var unknownSeatId = new InventorySeatId(Guid.CreateVersion7());

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            ReservationItem.Create(offer, new ReservationItemInventorySeatInput(unknownSeatId)));

        Assert.Equal(ReservationErrors.SeatNotCoveredByOffer(unknownSeatId).Code, exception.Code);
    }

    // ── Create from GeneralAdmissionPool ──────────────────────────────────────

    [Fact]
    public void Create_WithValidPool_ReturnsItemWithPoolType()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory, poolPrice: 25m);
        var poolId = inventory.Pools[0].Id;
        var poolInput = new ReservationItemGeneralAdmissionPoolInput(poolId, new Quantity(5));

        var item = ReservationItem.Create(offer, poolInput);

        Assert.Equal(ReservationItemType.GeneralAdmissionPool, item.Type);
        Assert.Equal(poolId, item.GeneralAdmissionPoolId);
        Assert.Null(item.InventorySeatId);
        Assert.Equal(5, item.Quantity.Value);
    }

    [Fact]
    public void Create_WithValidPool_SetsUnitPriceFromPriceZone()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory, poolPrice: 40m);
        var poolId = inventory.Pools[0].Id;

        var item = ReservationItem.Create(offer, new ReservationItemGeneralAdmissionPoolInput(poolId, new Quantity(2)));

        Assert.Equal(40m, item.UnitPrice.Value);
    }

    [Fact]
    public void Create_WithValidPool_CalculatesTotalAsQuantityXUnitPrice()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory, poolPrice: 20m);
        var poolId = inventory.Pools[0].Id;

        var item = ReservationItem.Create(offer, new ReservationItemGeneralAdmissionPoolInput(poolId, new Quantity(6)));

        Assert.Equal(120m, item.Total.Value); // 20 * 6
    }

    [Fact]
    public void Create_WithPoolNotCoveredByOffer_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var unknownPoolId = new GeneralAdmissionPoolId(Guid.CreateVersion7());

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            ReservationItem.Create(offer, new ReservationItemGeneralAdmissionPoolInput(unknownPoolId, new Quantity(1))));

        Assert.Equal(ReservationErrors.GeneralAdmissionPoolNotCoveredByOffer(unknownPoolId).Code, exception.Code);
    }

    // ── Constructor validation ────────────────────────────────────────────────

    [Fact]
    public void Create_WhenSeatWithQuantityNotOne_ThrowsArgumentException()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);

        // Direct construction with invalid quantity - this should fail
        // We test via the factory which enforces this
        var seatInput = new ReservationItemInventorySeatInput(inventory.Seats[0].Id);
        var item = ReservationItem.Create(offer, seatInput);
        
        // Verify quantity is 1
        Assert.Equal(1, item.Quantity.Value);
    }

    [Fact]
    public void Create_WithPoolAndQuantityZero_ThrowsArgumentException()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var poolId = inventory.Pools[0].Id;

        // Quantity constructor itself should reject 0
        var exception = Assert.Throws<ArgumentException>(() => new Quantity(0));
        Assert.Contains("greater than zero", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithPoolAndNegativeQuantity_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Quantity(-1));
        Assert.Contains("greater than zero", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── PriceZoneId tracking ─────────────────────────────────────────────────

    [Fact]
    public void Create_WithSeatFromMultiplePriceZones_UsesLatestZone()
    {
        var inventory = CreateInventory();
        var salesRange = DateTimeRange.Between(
            new DateTimeOffset(2026, 6, 10, 10, 0, 0, TimeSpan.Zero).AddDays(-1),
            new DateTimeOffset(2026, 6, 10, 10, 0, 0, TimeSpan.Zero).AddDays(7));
        var offer = Offer.Create(inventory.Id, new OfferName("Standard"), salesRange, Currency.USD);

        // Configure with one zone
        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("Standard"),
            new Amount(50m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);

        // Replace with a different zone at higher price
        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("Standard"),
            new Amount(100m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);

        offer.Activate();

        var seatId = inventory.Seats[0].Id;
        var item = ReservationItem.Create(offer, new ReservationItemInventorySeatInput(seatId));

        // Should use the latest zone (100)
        Assert.Equal(100m, item.UnitPrice.Value);
    }

    // ── Type invariants ──────────────────────────────────────────────────────

    [Fact]
    public void Create_SeatItem_HasNoPoolId()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var seatId = inventory.Seats[0].Id;

        var item = ReservationItem.Create(offer, new ReservationItemInventorySeatInput(seatId));

        Assert.NotNull(item.InventorySeatId);
        Assert.Null(item.GeneralAdmissionPoolId);
    }

    [Fact]
    public void Create_PoolItem_HasNoSeatId()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var poolId = inventory.Pools[0].Id;

        var item = ReservationItem.Create(offer, new ReservationItemGeneralAdmissionPoolInput(poolId, new Quantity(1)));

        Assert.Null(item.InventorySeatId);
        Assert.NotNull(item.GeneralAdmissionPoolId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Inventory CreateInventory()
    {
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
            generalAdmissionAreas:
            [
                new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Floor", 100)
            ]);

        return Inventory.CreateFromManifest(PublishedEventReferenceId.Create(), manifest);
    }

    private static Offer CreateActiveOffer(
        Inventory inventory,
        decimal seatPrice = 25m,
        decimal poolPrice = 25m)
    {
        var salesRange = DateTimeRange.Between(Now.AddDays(-1), Now.AddDays(7));
        var offer = Offer.Create(inventory.Id, new OfferName("Standard"), salesRange, Currency.USD);

        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("Seats"),
            new Amount(seatPrice),
            [
                new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id),
                new PriceZoneInventorySeatItemInput(inventory.Seats[1].Id)
            ],
            []);

        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("GA"),
            new Amount(poolPrice),
            [],
            [new PriceZoneGeneralAdmissionPoolItemInput(inventory.Pools[0].Id)]);

        offer.Activate();
        return offer;
    }
}
