using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Domain.Reservations;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Reservations;

public sealed class ReservationTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = Now.AddMinutes(15);

    // ── Create (seat) ─────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithSingleSeat_ReturnsReservationWithOneSeatItem()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var seatId = inventory.Seats[0].Id;

        var reservation = Reservation.Create(
            offer,
            [new ReservationItemInventorySeatInput(seatId)],
            [],
            Now,
            ExpiresAt);

        var item = Assert.Single(reservation.Items);
        Assert.Equal(ReservationItemType.Seat, item.Type);
        Assert.Equal(seatId, item.InventorySeatId);
        Assert.Equal(1, item.Quantity.Value);
    }

    [Fact]
    public void Create_WithSeatItem_CalculatesTotalFromPriceZone()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory, seatPrice: 50m);
        var seatId = inventory.Seats[0].Id;

        var reservation = Reservation.Create(
            offer,
            [new ReservationItemInventorySeatInput(seatId)],
            [],
            Now,
            ExpiresAt);

        Assert.Equal(50m, reservation.Total.Value);
    }

    [Fact]
    public void Create_WithMultipleSeats_SumsTotalAcrossItems()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory, seatPrice: 25m);
        var seat1 = inventory.Seats[0].Id;
        var seat2 = inventory.Seats[1].Id;

        var reservation = Reservation.Create(
            offer,
            [
                new ReservationItemInventorySeatInput(seat1),
                new ReservationItemInventorySeatInput(seat2)
            ],
            [],
            Now,
            ExpiresAt);

        Assert.Equal(2, reservation.Items.Count);
        Assert.Equal(50m, reservation.Total.Value);
    }

    [Fact]
    public void Create_WithDuplicateSeat_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var seatId = inventory.Seats[0].Id;

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            Reservation.Create(
                offer,
                [
                    new ReservationItemInventorySeatInput(seatId),
                    new ReservationItemInventorySeatInput(seatId)
                ],
                [],
                Now,
                ExpiresAt));

        Assert.Equal(ReservationErrors.DuplicateSeatsInReservation().Code, exception.Code);
    }

    [Fact]
    public void Create_WithSeatNotCoveredByOffer_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var unknownSeatId = new InventorySeatId(Guid.CreateVersion7());

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            Reservation.Create(
                offer,
                [new ReservationItemInventorySeatInput(unknownSeatId)],
                [],
                Now,
                ExpiresAt));

        Assert.Equal(ReservationErrors.SeatNotCoveredByOffer(unknownSeatId).Code, exception.Code);
    }

    // ── Create (general admission pool) ──────────────────────────────────────

    [Fact]
    public void Create_WithGeneralAdmissionPool_ReturnsReservationWithPoolItem()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var poolId = inventory.Pools[0].Id;

        var reservation = Reservation.Create(
            offer,
            [],
            [new ReservationItemGeneralAdmissionPoolInput(poolId, new Quantity(3))],
            Now,
            ExpiresAt);

        var item = Assert.Single(reservation.Items);
        Assert.Equal(ReservationItemType.GeneralAdmissionPool, item.Type);
        Assert.Equal(poolId, item.GeneralAdmissionPoolId);
        Assert.Equal(3, item.Quantity.Value);
    }

    [Fact]
    public void Create_WithGeneralAdmissionPool_CalculatesTotalFromQuantityAndPrice()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory, poolPrice: 20m);
        var poolId = inventory.Pools[0].Id;

        var reservation = Reservation.Create(
            offer,
            [],
            [new ReservationItemGeneralAdmissionPoolInput(poolId, new Quantity(4))],
            Now,
            ExpiresAt);

        Assert.Equal(80m, reservation.Total.Value);
    }

    [Fact]
    public void Create_WithDuplicateGeneralAdmissionPool_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var poolId = inventory.Pools[0].Id;

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            Reservation.Create(
                offer,
                [],
                [
                    new ReservationItemGeneralAdmissionPoolInput(poolId, new Quantity(1)),
                    new ReservationItemGeneralAdmissionPoolInput(poolId, new Quantity(2))
                ],
                Now,
                ExpiresAt));

        Assert.Equal(ReservationErrors.DuplicateGeneralAdmissionPoolsInReservation().Code, exception.Code);
    }

    [Fact]
    public void Create_WithPoolNotCoveredByOffer_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var unknownPoolId = new GeneralAdmissionPoolId(Guid.CreateVersion7());

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            Reservation.Create(
                offer,
                [],
                [new ReservationItemGeneralAdmissionPoolInput(unknownPoolId, new Quantity(1))],
                Now,
                ExpiresAt));

        Assert.Equal(ReservationErrors.GeneralAdmissionPoolNotCoveredByOffer(unknownPoolId).Code, exception.Code);
    }

    // ── Create (mixed) ────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithSeatAndPoolItems_ReturnsBothItems()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory, seatPrice: 30m, poolPrice: 10m);
        var seatId = inventory.Seats[0].Id;
        var poolId = inventory.Pools[0].Id;

        var reservation = Reservation.Create(
            offer,
            [new ReservationItemInventorySeatInput(seatId)],
            [new ReservationItemGeneralAdmissionPoolInput(poolId, new Quantity(2))],
            Now,
            ExpiresAt);

        Assert.Equal(2, reservation.Items.Count);
        Assert.Equal(50m, reservation.Total.Value); // 30 + 2*10
    }

    [Fact]
    public void Create_WithNoItems_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            Reservation.Create(offer, [], [], Now, ExpiresAt));

        Assert.Equal(ReservationErrors.ReservationMustHaveItems().Code, exception.Code);
    }

    // ── Create (offer validation) ─────────────────────────────────────────────

    [Fact]
    public void Create_WhenOfferIsNotActive_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateDraftOffer(inventory); // not activated

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            Reservation.Create(
                offer,
                [new ReservationItemInventorySeatInput(inventory.Seats[0].Id)],
                [],
                Now,
                ExpiresAt));

        Assert.Equal(ReservationErrors.OfferMustBeActiveToCreateReservation().Code, exception.Code);
    }

    [Fact]
    public void Create_WhenOfferIsNotOnSale_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        // Sales range ended before Now
        var pastSalesRange = DateTimeRange.Between(Now.AddDays(-10), Now.AddDays(-1));
        var offer = CreateActiveOffer(inventory, salesRange: pastSalesRange);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            Reservation.Create(
                offer,
                [new ReservationItemInventorySeatInput(inventory.Seats[0].Id)],
                [],
                Now,
                ExpiresAt));

        Assert.Equal(ReservationErrors.OfferNotOnSale().Code, exception.Code);
    }

    // ── Create (expiry validation) ────────────────────────────────────────────

    [Fact]
    public void Create_WhenExpiresAtIsInThePast_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);
        var pastExpiry = Now.AddMinutes(-1);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            Reservation.Create(
                offer,
                [new ReservationItemInventorySeatInput(inventory.Seats[0].Id)],
                [],
                Now,
                pastExpiry));

        Assert.Equal(ReservationErrors.ExpirationTimeMustBeInTheFuture().Code, exception.Code);
    }

    [Fact]
    public void Create_WhenExpiresAtEqualsNow_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            Reservation.Create(
                offer,
                [new ReservationItemInventorySeatInput(inventory.Seats[0].Id)],
                [],
                Now,
                expiresAt: Now)); // equal is also not in the future

        Assert.Equal(ReservationErrors.ExpirationTimeMustBeInTheFuture().Code, exception.Code);
    }

    // ── Create (initial state) ────────────────────────────────────────────────

    [Fact]
    public void Create_SetsInitialStatusToReserved()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);

        var reservation = Reservation.Create(
            offer,
            [new ReservationItemInventorySeatInput(inventory.Seats[0].Id)],
            [],
            Now,
            ExpiresAt);

        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
    }

    [Fact]
    public void Create_SetsOfferIdAndInventoryId()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);

        var reservation = Reservation.Create(
            offer,
            [new ReservationItemInventorySeatInput(inventory.Seats[0].Id)],
            [],
            Now,
            ExpiresAt);

        Assert.Equal(offer.Id, reservation.OfferId);
        Assert.Equal(inventory.Id, reservation.InventoryId);
    }

    [Fact]
    public void Create_SetsExpiresAtAndCurrency()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);

        var reservation = Reservation.Create(
            offer,
            [new ReservationItemInventorySeatInput(inventory.Seats[0].Id)],
            [],
            Now,
            ExpiresAt);

        Assert.Equal(ExpiresAt, reservation.ExpiresAt);
        Assert.Equal(Currency.USD, reservation.Currency);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenReserved_TransitionsToCancelled()
    {
        var reservation = CreateReservation();

        reservation.Cancel();

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsDomainRuleViolation()
    {
        var reservation = CreateReservation();
        reservation.Cancel();

        var exception = Assert.Throws<DomainRuleViolationException>(() => reservation.Cancel());

        Assert.Equal(ReservationErrors.ReservationIsNotInReservedStatus(reservation.Id).Code, exception.Code);
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsDomainRuleViolation()
    {
        var reservation = CreateReservation();
        reservation.Complete(Now);

        var exception = Assert.Throws<DomainRuleViolationException>(() => reservation.Cancel());

        Assert.Equal(ReservationErrors.ReservationIsNotInReservedStatus(reservation.Id).Code, exception.Code);
    }

    // ── Expire ────────────────────────────────────────────────────────────────

    [Fact]
    public void Expire_WhenReservationHasExpired_TransitionsToExpired()
    {
        var reservation = CreateReservation();
        var afterExpiry = ExpiresAt.AddSeconds(1);

        reservation.Expire(afterExpiry);

        Assert.Equal(ReservationStatus.Expired, reservation.Status);
    }

    [Fact]
    public void Expire_WhenReservationHasNotExpiredYet_ThrowsDomainRuleViolation()
    {
        var reservation = CreateReservation();
        var beforeExpiry = ExpiresAt.AddSeconds(-1);

        var exception = Assert.Throws<DomainRuleViolationException>(() => reservation.Expire(beforeExpiry));

        Assert.Equal(ReservationErrors.ReservationNotExpiredYet(reservation.Id).Code, exception.Code);
    }

    [Fact]
    public void Expire_WhenAlreadyCancelled_ThrowsDomainRuleViolation()
    {
        var reservation = CreateReservation();
        reservation.Cancel();
        var afterExpiry = ExpiresAt.AddSeconds(1);

        var exception = Assert.Throws<DomainRuleViolationException>(() => reservation.Expire(afterExpiry));

        Assert.Equal(ReservationErrors.ReservationIsNotInReservedStatus(reservation.Id).Code, exception.Code);
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_WhenReservedAndNotExpired_TransitionsToCompleted()
    {
        var reservation = CreateReservation();
        var beforeExpiry = ExpiresAt.AddSeconds(-1);

        reservation.Complete(beforeExpiry);

        Assert.Equal(ReservationStatus.Completed, reservation.Status);
    }

    [Fact]
    public void Complete_WhenReservationHasExpired_ThrowsDomainRuleViolation()
    {
        var reservation = CreateReservation();
        var afterExpiry = ExpiresAt.AddSeconds(1);

        var exception = Assert.Throws<DomainRuleViolationException>(() => reservation.Complete(afterExpiry));

        Assert.Equal(ReservationErrors.ReservationAlreadyExpired(reservation.Id).Code, exception.Code);
    }

    [Fact]
    public void Complete_WhenCancelled_ThrowsDomainRuleViolation()
    {
        var reservation = CreateReservation();
        reservation.Cancel();

        var exception = Assert.Throws<DomainRuleViolationException>(() => reservation.Complete(Now));

        Assert.Equal(ReservationErrors.ReservationIsNotInReservedStatus(reservation.Id).Code, exception.Code);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Reservation CreateReservation()
    {
        var inventory = CreateInventory();
        var offer = CreateActiveOffer(inventory);

        return Reservation.Create(
            offer,
            [new ReservationItemInventorySeatInput(inventory.Seats[0].Id)],
            [],
            Now,
            ExpiresAt);
    }

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

    private static Offer CreateDraftOffer(Inventory inventory, DateTimeRange? salesRange = null)
    {
        var range = salesRange ?? DateTimeRange.Between(Now.AddDays(-1), Now.AddDays(7));
        var offer = Offer.Create(inventory.Id, new OfferName("Standard"), range, Currency.USD);

        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("Main"),
            new Amount(25m),
            [
                new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id),
                new PriceZoneInventorySeatItemInput(inventory.Seats[1].Id)
            ],
            [new PriceZoneGeneralAdmissionPoolItemInput(inventory.Pools[0].Id)]);

        return offer;
    }

    private static Offer CreateActiveOffer(
        Inventory inventory,
        decimal seatPrice = 25m,
        decimal poolPrice = 25m,
        DateTimeRange? salesRange = null)
    {
        var range = salesRange ?? DateTimeRange.Between(Now.AddDays(-1), Now.AddDays(7));
        var offer = Offer.Create(inventory.Id, new OfferName("Standard"), range, Currency.USD);

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
