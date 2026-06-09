using VenuePass.Modules.Ticketing.Domain.Offers;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Offers;

public sealed class OfferValueObjectsTests
{
    [Fact]
    public void OfferName_WhenValueHasSurroundingWhitespace_TrimsValue()
    {
        var name = new OfferName("  Standard Offer  ");

        Assert.Equal("Standard Offer", name.Value);
    }

    [Fact]
    public void OfferName_WhenValueExceedsMaxLength_ThrowsArgumentException()
    {
        var value = new string('a', OfferName.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => _ = new OfferName(value));
    }

    [Fact]
    public void PriceLevelName_SameAs_IsCaseInsensitive()
    {
        var left = new PriceZoneName("General");
        var right = new PriceZoneName("general");

        Assert.True(left.SameAs(right));
    }

    [Fact]
    public void PriceLevelName_WhenValueHasSurroundingWhitespace_TrimsValue()
    {
        var name = new PriceZoneName("  VIP  ");

        Assert.Equal("VIP", name.Value);
    }

    [Fact]
    public void PriceLevelName_WhenValueExceedsMaxLength_ThrowsArgumentException()
    {
        var value = new string('a', PriceZoneName.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => _ = new PriceZoneName(value));
    }

    [Fact]
    public void Currency_WhenLowercaseValueProvided_NormalizesToUppercase()
    {
        var currency = new Currency("usd");

        Assert.Equal("USD", currency.Value);
    }

    [Fact]
    public void Currency_WhenLengthIsNotThree_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _ = new Currency("US"));
    }

    [Fact]
    public void Currency_WhenNonLettersIncluded_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _ = new Currency("U5D"));
    }

    [Fact]
    public void Amount_WhenNegative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new Amount(-0.01m));
    }

    [Fact]
    public void Amount_WhenValid_SetsValue()
    {
        var amount = new Amount(19.99m);

        Assert.Equal(19.99m, amount.Value);
    }
}
