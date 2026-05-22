using VenuePass.Modules.Events.Domain.Venues;
using Xunit;

namespace VenuePass.Modules.Events.Tests.Domain;

public sealed class VenueTests
{
    [Fact]
    public void VenueCreate_WhenAnyParameterIsNull_ThrowsArgumentNullException()
    {
        var validName = new VenueName("Main Hall");
        var validAddress = CreateAddress();
        var validCapacity = new VenueCapacity(250);

        Assert.Throws<ArgumentNullException>(() => Venue.Create(null!, validAddress, validCapacity));
        Assert.Throws<ArgumentNullException>(() => Venue.Create(validName, null!, validCapacity));
        Assert.Throws<ArgumentNullException>(() => Venue.Create(validName, validAddress, null!));
    }

    [Fact]
    public void VenueCreate_WhenValuesAreValid_CreatesVenue()
    {
        var name = new VenueName("Main Hall");
        var address = CreateAddress();
        var capacity = new VenueCapacity(250);

        var venue = Venue.Create(name, address, capacity);

        Assert.NotEqual(Guid.Empty, venue.Id);
        Assert.Equal(name, venue.Name);
        Assert.Equal(address, venue.Address);
        Assert.Equal(capacity, venue.Capacity);
    }

    private static VenueAddress CreateAddress()
    {
        return new VenueAddress(
            new StreetAddress("123 Main St"),
            new City("Seattle"),
            new Country("US"));
    }
}
