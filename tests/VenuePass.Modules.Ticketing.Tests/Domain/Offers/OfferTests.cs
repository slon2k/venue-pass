using VenuePass.BuildingBlocks.Domain;
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

        offer.ConfigurePriceLevel(
            inventory,
            new PriceLevelName("General"),
            [new PriceLevelInventorySeatItemInput(inventory.Seats[0].Id, new Amount(25m))],
            []);

        var priceLevel = Assert.Single(offer.PriceLevels);
        Assert.Equal("General", priceLevel.Name.Value);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenSameNameConfigured_ReplacesExistingCaseInsensitive()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);

        offer.ConfigurePriceLevel(
            inventory,
            new PriceLevelName("General"),
            [new PriceLevelInventorySeatItemInput(inventory.Seats[0].Id, new Amount(25m))],
            []);

        offer.ConfigurePriceLevel(
            inventory,
            new PriceLevelName("general"),
            [new PriceLevelInventorySeatItemInput(inventory.Seats[1].Id, new Amount(35m))],
            []);

        var priceLevel = Assert.Single(offer.PriceLevels);
        var item = Assert.Single(priceLevel.InventorySeatItems);
        Assert.Equal(35m, item.Price.Value);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenOfferIsNotDraft_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        offer.ConfigurePriceLevel(
            inventory,
            new PriceLevelName("General"),
            [new PriceLevelInventorySeatItemInput(inventory.Seats[0].Id, new Amount(25m))],
            []);
        offer.Activate();

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceLevel(
                inventory,
                new PriceLevelName("VIP"),
                [new PriceLevelInventorySeatItemInput(inventory.Seats[0].Id, new Amount(50m))],
                []));

        Assert.Equal(OfferErrors.CanOnlySetPriceLevelsInDraftStatus().Code, exception.Code);
        Assert.Equal(OfferErrors.CanOnlySetPriceLevelsInDraftStatus().Message, exception.Message);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenInventoryDoesNotBelongToOffer_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var otherInventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceLevel(
                otherInventory,
                new PriceLevelName("General"),
                [new PriceLevelInventorySeatItemInput(otherInventory.Seats[0].Id, new Amount(25m))],
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
            offer.ConfigurePriceLevel(
                inventory,
                new PriceLevelName("General"),
                [new PriceLevelInventorySeatItemInput(unknownSeatId, new Amount(25m))],
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
            offer.ConfigurePriceLevel(
                inventory,
                new PriceLevelName("General"),
                [],
                [new PriceLevelGeneralAdmissionPoolItemInput(unknownPoolId, new Amount(25m))]));

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
            offer.ConfigurePriceLevel(
                inventory,
                new PriceLevelName("General"),
                [
                    new PriceLevelInventorySeatItemInput(seatId, new Amount(20m)),
                    new PriceLevelInventorySeatItemInput(seatId, new Amount(22m))
                ],
                []));

        Assert.Equal(OfferErrors.PriceLevelCannotHaveDuplicateTargets().Code, exception.Code);
        Assert.Equal(OfferErrors.PriceLevelCannotHaveDuplicateTargets().Message, exception.Message);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenGeneralAdmissionTargetsDuplicate_ThrowsDomainRuleViolation()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        var poolId = inventory.Pools[0].Id;

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceLevel(
                inventory,
                new PriceLevelName("General"),
                [],
                [
                    new PriceLevelGeneralAdmissionPoolItemInput(poolId, new Amount(20m)),
                    new PriceLevelGeneralAdmissionPoolItemInput(poolId, new Amount(22m))
                ]));

        Assert.Equal(OfferErrors.PriceLevelCannotHaveDuplicateTargets().Code, exception.Code);
        Assert.Equal(OfferErrors.PriceLevelCannotHaveDuplicateTargets().Message, exception.Message);
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenNoPriceLevels_ThrowsDomainRuleViolation()
    {
        var offer = CreateOffer();

        var exception = Assert.Throws<DomainRuleViolationException>(() => offer.Activate());

        Assert.Equal(OfferErrors.OfferMustHaveAtLeastOnePriceLevelToActivate().Code, exception.Code);
        Assert.Equal(OfferErrors.OfferMustHaveAtLeastOnePriceLevelToActivate().Message, exception.Message);
    }

    [Fact]
    public void Activate_WhenPriceLevelExists_SetsStatusToActive()
    {
        var inventory = CreateInventory();
        var offer = CreateOffer(inventory.Id);
        offer.ConfigurePriceLevel(
            inventory,
            new PriceLevelName("General"),
            [new PriceLevelInventorySeatItemInput(inventory.Seats[0].Id, new Amount(25m))],
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
