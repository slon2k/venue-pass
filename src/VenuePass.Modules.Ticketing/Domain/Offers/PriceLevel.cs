using System.Globalization;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Ticketing.Domain.Inventories;

namespace VenuePass.Modules.Ticketing.Domain.Offers;

public sealed class PriceLevel : Entity<PriceLevelId>
{
    private readonly List<PriceLevelInventorySeatItem> _inventorySeatItems = [];
    private readonly List<PriceLevelGeneralAdmissionPoolItem> _generalAdmissionPoolItems = [];

    public PriceLevelName Name { get; private set; } = null!;

    public IReadOnlyList<PriceLevelInventorySeatItem> InventorySeatItems =>
        _inventorySeatItems.AsReadOnly();

    public IReadOnlyList<PriceLevelGeneralAdmissionPoolItem> GeneralAdmissionPoolItems =>
        _generalAdmissionPoolItems.AsReadOnly();

    private PriceLevel() { }

    private PriceLevel(
        PriceLevelId id,
        PriceLevelName name,
        IReadOnlyCollection<PriceLevelInventorySeatItem> inventorySeatItems,
        IReadOnlyCollection<PriceLevelGeneralAdmissionPoolItem> generalAdmissionPoolItems)
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
                OfferErrors.PriceLevelMustHaveAtLeastOneItem());
        }

        Name = name;

        _inventorySeatItems = [.. inventorySeatItems];
        _generalAdmissionPoolItems = [.. generalAdmissionPoolItems];
    }

    internal static PriceLevel Create(
        PriceLevelName name,
        IReadOnlyCollection<PriceLevelInventorySeatItemInput> inventorySeatItems,
        IReadOnlyCollection<PriceLevelGeneralAdmissionPoolItemInput> generalAdmissionPoolItems)
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
                OfferErrors.PriceLevelMustHaveAtLeastOneItem());
        }

        if (inventorySeatItems
            .GroupBy(item => item.InventorySeatId)
            .Any(group => group.Count() > 1))
        {
            throw new DomainRuleViolationException(
                OfferErrors.PriceLevelCannotHaveDuplicateTargets());
        }

        if (generalAdmissionPoolItems
            .GroupBy(item => item.GeneralAdmissionPoolId)
            .Any(group => group.Count() > 1))
        {
            throw new DomainRuleViolationException(
                OfferErrors.PriceLevelCannotHaveDuplicateTargets());
        }

        var seatItems = inventorySeatItems
            .Select(item => PriceLevelInventorySeatItem.Create(
                item.InventorySeatId,
                item.Price))
            .ToArray();

        var poolItems = generalAdmissionPoolItems
            .Select(item => PriceLevelGeneralAdmissionPoolItem.Create(
                item.GeneralAdmissionPoolId,
                item.Price))
            .ToArray();

        return new PriceLevel(
            id: PriceLevelId.Create(),
            name: name,
            inventorySeatItems: seatItems,
            generalAdmissionPoolItems: poolItems);
    }

    public bool HasItems =>
        _inventorySeatItems.Count > 0 ||
        _generalAdmissionPoolItems.Count > 0;
}

public readonly record struct PriceLevelId(Guid Value)
{
    public static PriceLevelId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(PriceLevelId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}

public sealed record PriceLevelName
{
    public const int MaxLength = 100;
    public string Value { get; }

    public PriceLevelName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(PriceLevelName name) => name.Value;

    public bool SameAs(PriceLevelName? other) => SameAs(this, other);

    public static bool SameAs(PriceLevelName? left, PriceLevelName? right) => (left, right) switch
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

public sealed class PriceLevelInventorySeatItem
{
    public InventorySeatId InventorySeatId { get; private set; }

    public Amount Price { get; private set; }

    private PriceLevelInventorySeatItem() { }

    private PriceLevelInventorySeatItem(
        InventorySeatId inventorySeatId,
        Amount price)
    {
        if (inventorySeatId.IsEmpty)
        {
            throw new ArgumentException(
                "Inventory seat ID cannot be empty.",
                nameof(inventorySeatId));
        }

        InventorySeatId = inventorySeatId;
        Price = price;
    }

    internal static PriceLevelInventorySeatItem Create(
        InventorySeatId inventorySeatId,
        Amount price)
    {
        return new PriceLevelInventorySeatItem(inventorySeatId, price);
    }
}

public sealed class PriceLevelGeneralAdmissionPoolItem
{
    public GeneralAdmissionPoolId GeneralAdmissionPoolId { get; private set; }

    public Amount Price { get; private set; }

    private PriceLevelGeneralAdmissionPoolItem() { }

    private PriceLevelGeneralAdmissionPoolItem(
        GeneralAdmissionPoolId generalAdmissionPoolId,
        Amount price)
    {
        if (generalAdmissionPoolId.IsEmpty)
        {
            throw new ArgumentException(
                "General admission pool ID cannot be empty.",
                nameof(generalAdmissionPoolId));
        }

        GeneralAdmissionPoolId = generalAdmissionPoolId;
        Price = price;
    }

    internal static PriceLevelGeneralAdmissionPoolItem Create(
        GeneralAdmissionPoolId generalAdmissionPoolId,
        Amount price)
    {
        return new PriceLevelGeneralAdmissionPoolItem(
            generalAdmissionPoolId,
            price);
    }
}

public sealed record PriceLevelInventorySeatItemInput
{
    public InventorySeatId InventorySeatId { get; }

    public Amount Price { get; }

    public PriceLevelInventorySeatItemInput(
        InventorySeatId inventorySeatId,
        Amount price)
    {
        if (inventorySeatId.IsEmpty)
        {
            throw new ArgumentException(
                "Inventory seat ID cannot be empty.",
                nameof(inventorySeatId));
        }

        InventorySeatId = inventorySeatId;
        Price = price;
    }
}

public sealed record PriceLevelGeneralAdmissionPoolItemInput
{
    public GeneralAdmissionPoolId GeneralAdmissionPoolId { get; }

    public Amount Price { get; }

    public PriceLevelGeneralAdmissionPoolItemInput(
        GeneralAdmissionPoolId generalAdmissionPoolId,
        Amount price)
    {
        if (generalAdmissionPoolId.IsEmpty)
        {
            throw new ArgumentException(
                "General admission pool ID cannot be empty.",
                nameof(generalAdmissionPoolId));
        }

        GeneralAdmissionPoolId = generalAdmissionPoolId;
        Price = price;
    }
}