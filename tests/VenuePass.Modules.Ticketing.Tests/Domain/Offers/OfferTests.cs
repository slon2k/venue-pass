using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Offers;

public sealed class OfferTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WhenInventoryIdIsEmpty_ThrowsArgumentException()
    {
        var salesRange = CreateSalesRange();

        void Act() => _ = Offer.Create(new InventoryId(Guid.Empty), new OfferName("Standard"), salesRange, Currency.USD);

        var exception = Assert.Throws<ArgumentException>(Act);
        Assert.Contains("InventoryId", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── ConfigurePriceLevel ───────────────────────────────────────────────────

    [Fact]
    public void ConfigurePriceLevel_WhenDraft_AddsPriceLevel()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);

        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("General"),
            new Amount(25m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);

        var priceLevel = Assert.Single(offer.PriceZones);
        Assert.Equal("General", priceLevel.Name.Value);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenSameNameConfigured_ReplacesExistingCaseInsensitive()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);

        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("General"),
            new Amount(25m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);

        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("general"),
            new Amount(35m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[1].Id)],
            []);

        var priceLevel = Assert.Single(offer.PriceZones);
        var item = Assert.Single(priceLevel.InventorySeatItems);
        Assert.Equal(35m, priceLevel.Price.Value);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenOfferIsNotDraft_ThrowsDomainConflict()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("General"),
            new Amount(25m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);
        offer.Activate();

        var exception = Assert.Throws<DomainConflictException>(() =>
            offer.ConfigurePriceZone(
                inventory,
                new PriceZoneName("VIP"),
                new Amount(50m),
                [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
                []));

        Assert.Equal(OfferErrors.CanOnlySetPriceZonesInDraftStatus().Code, exception.Code);
        Assert.Equal(OfferErrors.CanOnlySetPriceZonesInDraftStatus().Message, exception.Message);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenInventoryDoesNotBelongToOffer_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var otherInventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceZone(
                otherInventory,
                new PriceZoneName("General"),
                new Amount(25m),
                [new PriceZoneInventorySeatItemInput(otherInventory.Seats[0].Id)],
                []));

        Assert.Equal(OfferErrors.InventoryMismatch(otherInventory.Id.Value, inventory.Id.Value).Code, exception.Code);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenSeatNotInInventory_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        var unknownSeatId = new InventorySeatId(Guid.CreateVersion7());

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceZone(
                inventory,
                new PriceZoneName("General"),
                new Amount(25m),
                [new PriceZoneInventorySeatItemInput(unknownSeatId)],
                []));

        Assert.Equal(OfferErrors.SeatNotInInventory(unknownSeatId).Code, exception.Code);
        Assert.Equal(OfferErrors.SeatNotInInventory(unknownSeatId).Message, exception.Message);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenPoolNotInInventory_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        var unknownPoolId = new GeneralAdmissionPoolId(Guid.CreateVersion7());

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceZone(
                inventory,
                new PriceZoneName("General"),
                new Amount(25m),
                [],
                [new PriceZoneGeneralAdmissionPoolItemInput(unknownPoolId)]));

        Assert.Equal(OfferErrors.GeneralAdmissionPoolNotInInventory(unknownPoolId).Code, exception.Code);
        Assert.Equal(OfferErrors.GeneralAdmissionPoolNotInInventory(unknownPoolId).Message, exception.Message);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenInventorySeatTargetsDuplicate_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        var seatId = inventory.Seats[0].Id;

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceZone(
                inventory,
                new PriceZoneName("General"),
                new Amount(20m),
                [
                    new PriceZoneInventorySeatItemInput(seatId),
                    new PriceZoneInventorySeatItemInput(seatId)
                ],
                []));

        Assert.Equal(OfferErrors.PriceZoneCannotHaveDuplicateTargets().Code, exception.Code);
        Assert.Equal(OfferErrors.PriceZoneCannotHaveDuplicateTargets().Message, exception.Message);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenGeneralAdmissionTargetsDuplicate_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        var poolId = inventory.Pools[0].Id;

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceZone(
                inventory,
                new PriceZoneName("General"),
                new Amount(20m),
                [],
                [
                    new PriceZoneGeneralAdmissionPoolItemInput(poolId),
                    new PriceZoneGeneralAdmissionPoolItemInput(poolId)
                ]));

        Assert.Equal(OfferErrors.PriceZoneCannotHaveDuplicateTargets().Code, exception.Code);
        Assert.Equal(OfferErrors.PriceZoneCannotHaveDuplicateTargets().Message, exception.Message);
    }

    // ── SetPriceZones ─────────────────────────────────────────────────────────

    [Fact]
    public void SetPriceZones_WhenDraft_ReplacesPriceZones()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);

        // Start with two zones
        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("VIP"),
            new Amount(100m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);
        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("General"),
            new Amount(25m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[1].Id)],
            []);

        // Replace with one zone using a pool
        offer.SetPriceZones(inventory,
        [
            new PriceZoneInput(
                new PriceZoneName("Floor"),
                new Amount(50m),
                [],
                [new PriceZoneGeneralAdmissionPoolItemInput(inventory.Pools[0].Id)])
        ]);

        var zone = Assert.Single(offer.PriceZones);
        Assert.Equal("Floor", zone.Name.Value);
    }

    [Fact]
    public void SetPriceZones_WhenOfferIsNotDraft_ThrowsDomainConflict()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("General"),
            new Amount(25m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);
        offer.Activate();

        var exception = Assert.Throws<DomainConflictException>(() =>
            offer.SetPriceZones(inventory,
            [
                new PriceZoneInput(
                    new PriceZoneName("VIP"),
                    new Amount(50m),
                    [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
                    [])
            ]));

        Assert.Equal(OfferErrors.CanOnlySetPriceZonesInDraftStatus().Code, exception.Code);
    }

    [Fact]
    public void SetPriceZones_WhenEmptyList_ClearsAllZones()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("General"),
            new Amount(25m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);

        offer.SetPriceZones(inventory, []);

        Assert.Empty(offer.PriceZones);
    }

    [Fact]
    public void SetPriceZones_WhenDuplicateZoneNames_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.SetPriceZones(inventory,
            [
                new PriceZoneInput(
                    new PriceZoneName("VIP"),
                    new Amount(100m),
                    [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
                    []),
                new PriceZoneInput(
                    new PriceZoneName("vip"),
                    new Amount(50m),
                    [new PriceZoneInventorySeatItemInput(inventory.Seats[1].Id)],
                    [])
            ]));

        Assert.Equal(OfferErrors.DuplicatePriceZoneNames().Code, exception.Code);
    }

    [Fact]
    public void SetPriceZones_WhenSameSeatInTwoZones_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        var sharedSeatId = inventory.Seats[0].Id;

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.SetPriceZones(inventory,
            [
                new PriceZoneInput(
                    new PriceZoneName("Zone A"),
                    new Amount(100m),
                    [new PriceZoneInventorySeatItemInput(sharedSeatId)],
                    []),
                new PriceZoneInput(
                    new PriceZoneName("Zone B"),
                    new Amount(50m),
                    [new PriceZoneInventorySeatItemInput(sharedSeatId)],
                    [])
            ]));

        Assert.Equal(OfferErrors.InventorySeatAlreadyAssignedToAnotherPriceZone(sharedSeatId).Code, exception.Code);
    }

    [Fact]
    public void SetPriceZones_WhenSeatNotInInventory_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        var unknownSeatId = new InventorySeatId(Guid.CreateVersion7());

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.SetPriceZones(inventory,
            [
                new PriceZoneInput(
                    new PriceZoneName("VIP"),
                    new Amount(100m),
                    [new PriceZoneInventorySeatItemInput(unknownSeatId)],
                    [])
            ]));

        Assert.Equal(OfferErrors.SeatNotInInventory(unknownSeatId).Code, exception.Code);
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenNoPriceLevels_ThrowsDomainRuleViolation()
    {
        var offer = CreateOffer();

        var exception = Assert.Throws<DomainRuleViolationException>(() => offer.Activate());

        Assert.Equal(OfferErrors.OfferMustHaveAtLeastOnePriceZoneToActivate().Code, exception.Code);
        Assert.Equal(OfferErrors.OfferMustHaveAtLeastOnePriceZoneToActivate().Message, exception.Message);
    }

    [Fact]
    public void Activate_WhenPriceLevelExists_SetsStatusToActive()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("General"),
            new Amount(25m),
            [new PriceZoneInventorySeatItemInput(inventory.Seats[0].Id)],
            []);

        offer.Activate();

        Assert.Equal(OfferStatus.Active, offer.Status);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Offer CreateOffer() =>
        Offer.Create(new InventoryId(Guid.CreateVersion7()), new OfferName("Standard"), CreateSalesRange(), Currency.USD);

    private static Offer CreateOffer(InventoryId inventoryId) =>
        Offer.Create(inventoryId, new OfferName("Standard"), CreateSalesRange(), Currency.USD);

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
            generalAdmissionAreas: [new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Floor", 100)]);
        return Inventory.CreateFromManifest(PublishedEventReferenceId.Create(), manifest);
    }

    private static DateTimeRange CreateSalesRange()
    {
        var start = new DateTimeOffset(2026, 6, 8, 10, 0, 0, TimeSpan.Zero);
        var end = start.AddDays(7);
        return new DateTimeRange(start, end);
    }
}
