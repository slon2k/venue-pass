using System.Globalization;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Ticketing.Domain.Inventories;

namespace VenuePass.Modules.Ticketing.Domain.Offers;

public sealed class PriceZone : Entity<PriceZoneId>
{
    private readonly List<PriceZoneInventorySeatItem> _inventorySeatItems = [];
    private readonly List<PriceZoneGeneralAdmissionPoolItem> _generalAdmissionPoolItems = [];

    public PriceZoneName Name { get; private set; } = null!;

    public Amount Price { get; private set; }

    public IReadOnlyList<PriceZoneInventorySeatItem> InventorySeatItems =>
        _inventorySeatItems.AsReadOnly();

    public IReadOnlyList<PriceZoneGeneralAdmissionPoolItem> GeneralAdmissionPoolItems =>
        _generalAdmissionPoolItems.AsReadOnly();

    private PriceZone() { }

    private PriceZone(
        PriceZoneId id,
        PriceZoneName name,
        Amount price,
        IReadOnlyCollection<PriceZoneInventorySeatItem> inventorySeatItems,
        IReadOnlyCollection<PriceZoneGeneralAdmissionPoolItem> generalAdmissionPoolItems)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(inventorySeatItems);
        ArgumentNullException.ThrowIfNull(generalAdmissionPoolItems);

        if (inventorySeatItems.Any(item => item is null))
        {
            throw new ArgumentException(
                "Inventory seat item list cannot contain null items.",
                nameof(inventorySeatItems));
        }

        if (generalAdmissionPoolItems.Any(item => item is null))
        {
            throw new ArgumentException(
                "General admission pool item list cannot contain null items.",
                nameof(generalAdmissionPoolItems));
        }

        if (inventorySeatItems.Count == 0 && generalAdmissionPoolItems.Count == 0)
        {
            throw new DomainRuleViolationException(
                OfferErrors.PriceZoneMustHaveAtLeastOneItem());
        }

        Name = name;
        Price = price;

        _inventorySeatItems = [.. inventorySeatItems];
        _generalAdmissionPoolItems = [.. generalAdmissionPoolItems];
    }

    internal static PriceZone Create(
        PriceZoneName name,
        Amount price,
        IReadOnlyCollection<PriceZoneInventorySeatItemInput> inventorySeatItems,
        IReadOnlyCollection<PriceZoneGeneralAdmissionPoolItemInput> generalAdmissionPoolItems)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(inventorySeatItems);
        ArgumentNullException.ThrowIfNull(generalAdmissionPoolItems);

        if (inventorySeatItems.Any(item => item is null))
        {
            throw new ArgumentException(
                "Inventory seat item list cannot contain null items.",
                nameof(inventorySeatItems));
        }

        if (generalAdmissionPoolItems.Any(item => item is null))
        {
            throw new ArgumentException(
                "General admission pool item list cannot contain null items.",
                nameof(generalAdmissionPoolItems));
        }

        if (inventorySeatItems.Count == 0 && generalAdmissionPoolItems.Count == 0)
        {
            throw new DomainRuleViolationException(
                OfferErrors.PriceZoneMustHaveAtLeastOneItem());
        }

        if (inventorySeatItems
            .GroupBy(item => item.InventorySeatId)
            .Any(group => group.Count() > 1))
        {
            throw new DomainRuleViolationException(
                OfferErrors.PriceZoneCannotHaveDuplicateTargets());
        }

        if (generalAdmissionPoolItems
            .GroupBy(item => item.GeneralAdmissionPoolId)
            .Any(group => group.Count() > 1))
        {
            throw new DomainRuleViolationException(
                OfferErrors.PriceZoneCannotHaveDuplicateTargets());
        }

        var seatItems = inventorySeatItems
            .Select(item => PriceZoneInventorySeatItem.Create(item.InventorySeatId))
            .ToArray();

        var poolItems = generalAdmissionPoolItems
            .Select(item => PriceZoneGeneralAdmissionPoolItem.Create(item.GeneralAdmissionPoolId))
            .ToArray();

        return new PriceZone(
            id: PriceZoneId.Create(),
            price: price,
            name: name,
            inventorySeatItems: seatItems,
            generalAdmissionPoolItems: poolItems);
    }

    public bool HasItems =>
        _inventorySeatItems.Count > 0 ||
        _generalAdmissionPoolItems.Count > 0;
}

public readonly record struct PriceZoneId(Guid Value)
{
    public static PriceZoneId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(PriceZoneId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}

public sealed record PriceZoneName
{
    public const int MaxLength = 100;
    public string Value { get; }

    public PriceZoneName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(PriceZoneName name) => name.Value;

    public bool SameAs(PriceZoneName? other) => SameAs(this, other);

    public static bool SameAs(PriceZoneName? left, PriceZoneName? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        _ => string.Equals(left.Value, right.Value, StringComparison.OrdinalIgnoreCase)
    };

    public override string ToString() => Value;
}

public sealed record Currency
{
    public const int Length = 3;

    public string Value { get; }

    public Currency(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim().ToUpperInvariant();

        if (value.Length != Length || value.Any(c => c is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Currency must be a 3-letter ISO currency code.",
                nameof(value));
        }

        Value = value;
    }

    public static implicit operator string(Currency currency) => currency.Value;

    public override string ToString() => Value;

    public static readonly Currency USD = new("USD");
    public static readonly Currency EUR = new("EUR");
    public static readonly Currency GBP = new("GBP");
    public static readonly Currency JPY = new("JPY");
}

public readonly record struct Amount
{
    public decimal Value { get; }
    public Amount(decimal value)
    {
        value.ThrowIfNotInRange(nameof(value), 0.0m, decimal.MaxValue);
        Value = value;
    }

    public static implicit operator decimal(Amount amount) => amount.Value;
    public override string ToString() => Value.ToString("F2", CultureInfo.InvariantCulture);
}

public sealed class PriceZoneInventorySeatItem
{
    public InventorySeatId InventorySeatId { get; private set; }

    private PriceZoneInventorySeatItem() { }

    private PriceZoneInventorySeatItem(InventorySeatId inventorySeatId)
    {
        if (inventorySeatId.IsEmpty)
        {
            throw new ArgumentException(
                "Inventory seat ID cannot be empty.",
                nameof(inventorySeatId));
        }

        InventorySeatId = inventorySeatId;
    }

    internal static PriceZoneInventorySeatItem Create(InventorySeatId inventorySeatId) => new(inventorySeatId);
}

public sealed class PriceZoneGeneralAdmissionPoolItem
{
    public GeneralAdmissionPoolId GeneralAdmissionPoolId { get; private set; }

    private PriceZoneGeneralAdmissionPoolItem() { }

    private PriceZoneGeneralAdmissionPoolItem(GeneralAdmissionPoolId generalAdmissionPoolId)
    {
        if (generalAdmissionPoolId.IsEmpty)
        {
            throw new ArgumentException(
                "General admission pool ID cannot be empty.",
                nameof(generalAdmissionPoolId));
        }

        GeneralAdmissionPoolId = generalAdmissionPoolId;
    }

    internal static PriceZoneGeneralAdmissionPoolItem Create(GeneralAdmissionPoolId generalAdmissionPoolId) => new(generalAdmissionPoolId);
}

public sealed record PriceZoneInventorySeatItemInput
{
    public InventorySeatId InventorySeatId { get; }

    public PriceZoneInventorySeatItemInput(InventorySeatId inventorySeatId)
    {
        if (inventorySeatId.IsEmpty)
        {
            throw new ArgumentException(
                "Inventory seat ID cannot be empty.",
                nameof(inventorySeatId));
        }

        InventorySeatId = inventorySeatId;
    }
}

public sealed record PriceZoneGeneralAdmissionPoolItemInput
{
    public GeneralAdmissionPoolId GeneralAdmissionPoolId { get; }

    public PriceZoneGeneralAdmissionPoolItemInput(GeneralAdmissionPoolId generalAdmissionPoolId)
    {
        if (generalAdmissionPoolId.IsEmpty)
        {
            throw new ArgumentException(
                "General admission pool ID cannot be empty.",
                nameof(generalAdmissionPoolId));
        }

        GeneralAdmissionPoolId = generalAdmissionPoolId;
    }
}