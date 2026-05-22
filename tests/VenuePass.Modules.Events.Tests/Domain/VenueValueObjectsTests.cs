using VenuePass.Modules.Events.Domain.Venues;
using Xunit;

namespace VenuePass.Modules.Events.Tests.Domain;

public sealed class VenueValueObjectsTests
{
    [Fact]
    public void VenueName_WhenCreated_TrimsValue()
    {
        var name = new VenueName("  Main Hall  ");

        Assert.Equal("Main Hall", name.Value);
    }

    [Fact]
    public void VenueName_WhenNull_ThrowsArgumentNullException()
    {
        void Act() => _ = new VenueName(null!);

        Assert.Throws<ArgumentNullException>(Act);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void VenueName_WhenEmptyOrWhitespace_ThrowsArgumentException(string value)
    {
        void Act() => _ = new VenueName(value);

        Assert.Throws<ArgumentException>(Act);
    }

    [Fact]
    public void VenueName_WhenTooLong_ThrowsArgumentException()
    {
        var tooLongName = new string('a', VenueName.MaxLength + 1);

        void Act() => _ = new VenueName(tooLongName);

        Assert.Throws<ArgumentException>(Act);
    }

    [Theory]
    [InlineData("  Seattle  ", "Seattle")]
    [InlineData("  US  ", "US")]
    [InlineData("  123 Main St  ", "123 Main St")]
    public void AddressPrimitives_WhenCreated_TrimValue(string raw, string expected)
    {
        var city = new City(raw);
        var country = new Country(raw);
        var streetAddress = new StreetAddress(raw);

        Assert.Equal(expected, city.Value);
        Assert.Equal(expected, country.Value);
        Assert.Equal(expected, streetAddress.Value);
    }

    [Fact]
    public void VenueCapacity_WhenNotPositive_ThrowsArgumentOutOfRangeException()
    {
        void Act() => _ = new VenueCapacity(0);

        Assert.Throws<ArgumentOutOfRangeException>(Act);
    }

    [Fact]
    public void VenueAddress_WhenAnyParameterIsNull_ThrowsArgumentNullException()
    {
        var validStreet = new StreetAddress("123 Main St");
        var validCity = new City("Seattle");
        var validCountry = new Country("US");

        Assert.Throws<ArgumentNullException>(() => new VenueAddress(null!, validCity, validCountry));
        Assert.Throws<ArgumentNullException>(() => new VenueAddress(validStreet, null!, validCountry));
        Assert.Throws<ArgumentNullException>(() => new VenueAddress(validStreet, validCity, null!));
    }
}
