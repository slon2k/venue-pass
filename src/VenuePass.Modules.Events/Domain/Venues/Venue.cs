using System.Globalization;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.Venues;

public sealed class Venue : AggregateRoot<VenueId>
{
    private Venue()
    {
    }

    private Venue(VenueId id, VenueName name, VenueAddress address, VenueCapacity capacity)
        : base(id)
    {
        Name = name;
        Address = address;
        Capacity = capacity;
    }

    public VenueName Name { get; private set; } = null!;
    public VenueAddress Address { get; private set; } = null!;
    public VenueCapacity Capacity { get; private set; } = null!;

    public static Venue Create(VenueName name, VenueAddress address, VenueCapacity capacity)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(capacity);

        return new Venue(VenueId.Create(), name, address, capacity);
    }
}

public readonly record struct VenueId(Guid Value)
{
    public static VenueId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(VenueId id) => id.Value;
    public override string ToString() => Value.ToString();
}

public sealed record VenueName
{
    public const int MaxLength = 100;
    public string Value { get; private set; }

    public VenueName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(VenueName name) => name.Value;

    public override string ToString() => Value;
}

public sealed record VenueCapacity
{
    public int Value { get; private set; }

    public VenueCapacity(int value)
    {
        value.ThrowIfNotInRange(nameof(value), 1, int.MaxValue);
        Value = value;
    }

    public static implicit operator int(VenueCapacity capacity) => capacity.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public sealed record VenueAddress
{
    public StreetAddress StreetAddress { get; private set; }
    public City City { get; private set; }
    public Country Country { get; private set; }

    public VenueAddress(StreetAddress streetAddress, City city, Country country)
    {
        ArgumentNullException.ThrowIfNull(streetAddress);
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(country);

        StreetAddress = streetAddress;
        City = city;
        Country = country;
    }

    public string FullAddress => $"{StreetAddress}, {City}, {Country}";

    public override string ToString() => FullAddress;
}

public sealed record Country
{
    public const int MaxLength = 100;
    public string Value { get; private set; }

    public Country(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(Country country) => country.Value;

    public override string ToString() => Value;
}

public sealed record City
{
    public const int MaxLength = 100;
    public string Value { get; private set; }

    public City(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(City city) => city.Value;

    public override string ToString() => Value;
}

public sealed record StreetAddress
{
    public const int MaxLength = 200;
    public string Value { get; private set; }

    public StreetAddress(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(StreetAddress streetAddress) => streetAddress.Value;

    public override string ToString() => Value;
}
