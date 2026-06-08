using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Offers;

public sealed class OfferTests
{
    [Fact]
    public void Create_WhenInventoryIdIsEmpty_ThrowsArgumentException()
    {
        var salesRange = CreateSalesRange();

        void Act() => _ = Offer.Create(new InventoryId(Guid.Empty), new OfferName("Standard"), salesRange, Currency.USD);

        var exception = Assert.Throws<ArgumentException>(Act);
        Assert.Contains("InventoryId", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenDraft_AddsPriceLevel()
    {
        var offer = CreateOffer();

        offer.ConfigurePriceLevel(
            new PriceLevelName("General"),
            [new PriceLevelInventorySeatItemInput(new InventorySeatId(Guid.CreateVersion7()), new Amount(25m))],
            []);

        var priceLevel = Assert.Single(offer.PriceLevels);
        Assert.Equal("General", priceLevel.Name.Value);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenSameNameConfigured_ReplacesExistingCaseInsensitive()
    {
        var offer = CreateOffer();

        offer.ConfigurePriceLevel(
            new PriceLevelName("General"),
            [new PriceLevelInventorySeatItemInput(new InventorySeatId(Guid.CreateVersion7()), new Amount(25m))],
            []);

        offer.ConfigurePriceLevel(
            new PriceLevelName("general"),
            [new PriceLevelInventorySeatItemInput(new InventorySeatId(Guid.CreateVersion7()), new Amount(35m))],
            []);

        var priceLevel = Assert.Single(offer.PriceLevels);
        var item = Assert.Single(priceLevel.InventorySeatItems);
        Assert.Equal(35m, item.Price.Value);
    }

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
        var offer = CreateOffer();
        offer.ConfigurePriceLevel(
            new PriceLevelName("General"),
            [new PriceLevelInventorySeatItemInput(new InventorySeatId(Guid.CreateVersion7()), new Amount(25m))],
            []);

        offer.Activate();

        Assert.Equal(OfferStatus.Active, offer.Status);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenOfferIsNotDraft_ThrowsDomainRuleViolation()
    {
        var offer = CreateOffer();
        offer.ConfigurePriceLevel(
            new PriceLevelName("General"),
            [new PriceLevelInventorySeatItemInput(new InventorySeatId(Guid.CreateVersion7()), new Amount(25m))],
            []);
        offer.Activate();

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceLevel(
                new PriceLevelName("VIP"),
                [new PriceLevelInventorySeatItemInput(new InventorySeatId(Guid.CreateVersion7()), new Amount(50m))],
                []));

        Assert.Equal(OfferErrors.CanOnlySetPriceLevelsInDraftStatus().Code, exception.Code);
        Assert.Equal(OfferErrors.CanOnlySetPriceLevelsInDraftStatus().Message, exception.Message);
    }

    [Fact]
    public void ConfigurePriceLevel_WhenInventorySeatTargetsDuplicate_ThrowsDomainRuleViolation()
    {
        var offer = CreateOffer();
        var seatId = new InventorySeatId(Guid.CreateVersion7());

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceLevel(
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
        var offer = CreateOffer();
        var poolId = new GeneralAdmissionPoolId(Guid.CreateVersion7());

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            offer.ConfigurePriceLevel(
                new PriceLevelName("General"),
                [],
                [
                    new PriceLevelGeneralAdmissionPoolItemInput(poolId, new Amount(20m)),
                    new PriceLevelGeneralAdmissionPoolItemInput(poolId, new Amount(22m))
                ]));

        Assert.Equal(OfferErrors.PriceLevelCannotHaveDuplicateTargets().Code, exception.Code);
        Assert.Equal(OfferErrors.PriceLevelCannotHaveDuplicateTargets().Message, exception.Message);
    }

    private static Offer CreateOffer()
    {
        return Offer.Create(
            new InventoryId(Guid.CreateVersion7()),
            new OfferName("Standard"),
            CreateSalesRange(),
            Currency.USD);
    }

    private static DateTimeRange CreateSalesRange()
    {
        var start = new DateTimeOffset(2026, 6, 8, 10, 0, 0, TimeSpan.Zero);
        var end = start.AddDays(7);
        return new DateTimeRange(start, end);
    }
}
