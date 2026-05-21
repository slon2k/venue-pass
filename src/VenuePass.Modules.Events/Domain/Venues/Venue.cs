using System.Globalization;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.Venues;

public sealed class Venue : AggregateRoot<Guid>
{
    public VenueName Name { get; private set; } = null!;
    public VenueAddress Address { get; private set; } = null!;
    public VenueCapacity Capacity { get; private set; } = null!;

    private Venue() { }

    private Venue(Guid id, VenueName name, VenueAddress address, VenueCapacity capacity)
    {
        Id = id;
        Name = name;
        Address = address;
        Capacity = capacity;
    }

    public static Venue Create(VenueName name, VenueAddress address, VenueCapacity capacity)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(capacity);

        return new Venue(Guid.CreateVersion7(), name, address, capacity);
    }
}

public sealed record VenueName
{
    public const int MaxLength = 100;
    public string Value { get; }

    public VenueName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public override string ToString() => Value;
}

public sealed record VenueAddress
{
    public StreetAddress StreetAddress { get; }
    public City City { get; }
    public Country Country { get; }

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
    public string Value { get; }

    public Country(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public override string ToString() => Value;
}

public sealed record City
{
    public const int MaxLength = 100;
    public string Value { get; }

    public City(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public override string ToString() => Value;
}

public sealed record StreetAddress
{
    public const int MaxLength = 200;
    public string Value { get; }

    public StreetAddress(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public override string ToString() => Value;
}

public sealed record VenueCapacity
{
    public int Value { get; }

    public VenueCapacity(int value)
    {
        value.ThrowIfNotInRange(nameof(value), 1, int.MaxValue);
        Value = value;
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}