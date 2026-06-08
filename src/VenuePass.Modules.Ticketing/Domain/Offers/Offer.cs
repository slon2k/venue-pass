using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Ticketing.Domain.Inventories;

namespace VenuePass.Modules.Ticketing.Domain.Offers;

public sealed class Offer : AggregateRoot<OfferId>
{
    private readonly List<PriceLevel> _priceLevels = [];

    public InventoryId InventoryId { get; private set; }

    public OfferName Name { get; private set; } = null!;

    public Currency Currency { get; private set; } = null!;

    public OfferStatus Status { get; private set; } = OfferStatus.Draft;

    public DateTimeRange SalesRange { get; private set; }

    public IReadOnlyList<PriceLevel> PriceLevels => _priceLevels.AsReadOnly();

    private Offer() { }

    private Offer(OfferId id, InventoryId inventoryId, OfferName name, DateTimeRange salesRange, Currency currency) : base(id)
    {
        InventoryId = inventoryId;
        Name = name;
        SalesRange = salesRange;
        Status = OfferStatus.Draft;
        Currency = currency;
    }

    public static Offer Create(InventoryId inventoryId, OfferName name, DateTimeRange salesRange, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(currency);

        if (inventoryId.IsEmpty)
        {
            throw new ArgumentException("InventoryId cannot be empty.", nameof(inventoryId));
        }

        return new Offer(
            id: OfferId.Create(),
            inventoryId: inventoryId,
            name: name,
            salesRange: salesRange,
            currency: currency
        );
    }

    public void ConfigurePriceLevel(
        PriceLevelName priceLevelName,
        IEnumerable<PriceLevelInventorySeatItemInput> inventorySeatItems,
        IEnumerable<PriceLevelGeneralAdmissionPoolItemInput> generalAdmissionPoolItems)
    {
        ArgumentNullException.ThrowIfNull(priceLevelName);
        ArgumentNullException.ThrowIfNull(inventorySeatItems);
        ArgumentNullException.ThrowIfNull(generalAdmissionPoolItems);

        EnsureDraft();

        var seatItems = inventorySeatItems.ToArray();
        var poolItems = generalAdmissionPoolItems.ToArray();

        var priceLevel = PriceLevel.Create(
            priceLevelName,
            seatItems,
            poolItems);

        _priceLevels.RemoveAll(pl => pl.Name.SameAs(priceLevelName));

        _priceLevels.Add(priceLevel);
    }

    public void Activate()
    {
        if (Status != OfferStatus.Draft)
        {
            throw new DomainRuleViolationException(OfferErrors.CanOnlyActivateOfferInDraftStatus());
        }

        if (_priceLevels.Count == 0)
        {
            throw new DomainRuleViolationException(OfferErrors.OfferMustHaveAtLeastOnePriceLevelToActivate());
        }

        foreach (var priceLevel in _priceLevels)
        {
            if (!priceLevel.HasItems)
            {
                throw new DomainRuleViolationException(OfferErrors.PriceLevelMustHaveAtLeastOneItem());
            }
        }

        Status = OfferStatus.Active;
    }

    private void EnsureDraft()
    {
        if (Status != OfferStatus.Draft)
        {
            throw new DomainRuleViolationException(OfferErrors.CanOnlySetPriceLevelsInDraftStatus());
        }
    }
}

public readonly record struct OfferId(Guid Value)
{
    public static OfferId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(OfferId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}

public sealed record OfferName
{
    public const int MaxLength = 100;
    public string Value { get; }
    
    public OfferName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(OfferName name) => name.Value;
    public override string ToString() => Value;
}

public enum OfferStatus
{
    Draft = 0,
    Active = 1,
    Closed = 2
}
