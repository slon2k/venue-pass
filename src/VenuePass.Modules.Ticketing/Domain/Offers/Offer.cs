using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;

namespace VenuePass.Modules.Ticketing.Domain.Offers;

public sealed class Offer : AggregateRoot<OfferId>
{
    private readonly List<PriceZone> _priceZones = [];

    public InventoryId InventoryId { get; private set; }

    public OfferName Name { get; private set; } = null!;

    public Currency Currency { get; private set; } = null!;

    public OfferStatus Status { get; private set; } = OfferStatus.Draft;

    public DateTimeRange SalesRange { get; private set; }

    public IReadOnlyList<PriceZone> PriceZones => _priceZones.AsReadOnly();

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

    public void ConfigurePriceZone(
        Inventory inventory,
        PriceZoneName priceZoneName,
        Amount price,
        IEnumerable<PriceZoneInventorySeatItemInput> inventorySeatItems,
        IEnumerable<PriceZoneGeneralAdmissionPoolItemInput> generalAdmissionPoolItems)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(priceZoneName);
        ArgumentNullException.ThrowIfNull(inventorySeatItems);
        ArgumentNullException.ThrowIfNull(generalAdmissionPoolItems);

        EnsureDraft();
        EnsureCorrectInventory(inventory);

        var seatItems = inventorySeatItems.ToArray();
        var poolItems = generalAdmissionPoolItems.ToArray();

        var priceZone = PriceZone.Create(
            name: priceZoneName,
            price: price,
            inventorySeatItems: seatItems,
            generalAdmissionPoolItems: poolItems);

        EnsureSeatsAndPoolsExistInInventory(inventory, priceZone);
        EnsureTargetsAreNotAssignedToOtherPriceZones(priceZoneName, priceZone);

        _priceZones.RemoveAll(pz => pz.Name.SameAs(priceZoneName));
        _priceZones.Add(priceZone);
    }

    public void SetPriceZones(Inventory inventory, IReadOnlyList<PriceZoneInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(inputs);

        EnsureDraft();
        EnsureCorrectInventory(inventory);

        // Reject duplicate zone names within the input
        var duplicateNames = inputs
            .GroupBy(i => i.Name.Value, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicateNames.Length > 0)
            throw new DomainRuleViolationException(OfferErrors.DuplicatePriceZoneNames());

        // Create all zones (PriceZone.Create validates each zone individually)
        var newZones = inputs
            .Select(i => PriceZone.Create(i.Name, i.Price, i.InventorySeatItems, i.GeneralAdmissionPoolItems))
            .ToArray();

        // Validate all zones against the inventory
        foreach (var zone in newZones)
            EnsureSeatsAndPoolsExistInInventory(inventory, zone);

        // Check cross-zone conflicts within the new set
        var allSeatIds = new HashSet<InventorySeatId>();
        var allPoolIds = new HashSet<GeneralAdmissionPoolId>();

        foreach (var zone in newZones)
        {
            foreach (var seatItem in zone.InventorySeatItems)
            {
                if (!allSeatIds.Add(seatItem.InventorySeatId))
                    throw new DomainRuleViolationException(
                        OfferErrors.InventorySeatAlreadyAssignedToAnotherPriceZone(seatItem.InventorySeatId));
            }
            foreach (var poolItem in zone.GeneralAdmissionPoolItems)
            {
                if (!allPoolIds.Add(poolItem.GeneralAdmissionPoolId))
                    throw new DomainRuleViolationException(
                        OfferErrors.GeneralAdmissionPoolAlreadyAssignedToAnotherPriceZone(poolItem.GeneralAdmissionPoolId));
            }
        }

        _priceZones.Clear();
        _priceZones.AddRange(newZones);
    }

    private void EnsureTargetsAreNotAssignedToOtherPriceZones(PriceZoneName priceZoneName, PriceZone candidate)
    {
        var candidateSeatIds = candidate.InventorySeatItems
            .Select(item => item.InventorySeatId)
            .ToHashSet();

        var candidatePoolIds = candidate.GeneralAdmissionPoolItems
            .Select(item => item.GeneralAdmissionPoolId)
            .ToHashSet();

        foreach (var existingPriceZone in _priceZones)
        {
            if (existingPriceZone.Name.SameAs(priceZoneName))
            {
                continue;
            }

            foreach (var existingSeatItem in existingPriceZone.InventorySeatItems)
            {
                if (candidateSeatIds.Contains(existingSeatItem.InventorySeatId))
                {
                    throw new DomainRuleViolationException(
                        OfferErrors.InventorySeatAlreadyAssignedToAnotherPriceZone(existingSeatItem.InventorySeatId));
                }
            }

            foreach (var existingPoolItem in existingPriceZone.GeneralAdmissionPoolItems)
            {
                if (candidatePoolIds.Contains(existingPoolItem.GeneralAdmissionPoolId))
                {
                    throw new DomainRuleViolationException(
                        OfferErrors.GeneralAdmissionPoolAlreadyAssignedToAnotherPriceZone(existingPoolItem.GeneralAdmissionPoolId));
                }
            }
        }
    }

    private static void EnsureSeatsAndPoolsExistInInventory(
        Inventory inventory,
        PriceZone priceZone)
    {
        var seatIds = inventory.Seats
            .Select(seat => seat.Id)
            .ToHashSet();

        var poolIds = inventory.Pools
            .Select(pool => pool.Id)
            .ToHashSet();

        foreach (var item in priceZone.InventorySeatItems)
        {
            if (!seatIds.Contains(item.InventorySeatId))
            {
                throw new DomainRuleViolationException(
                    OfferErrors.SeatNotInInventory(item.InventorySeatId));
            }
        }

        foreach (var item in priceZone.GeneralAdmissionPoolItems)
        {
            if (!poolIds.Contains(item.GeneralAdmissionPoolId))
            {
                throw new DomainRuleViolationException(
                    OfferErrors.GeneralAdmissionPoolNotInInventory(item.GeneralAdmissionPoolId));
            }
        }
    }

    private void EnsureCorrectInventory(Inventory inventory)
    {
        if (inventory.Id != InventoryId)
        {
            throw new DomainRuleViolationException(OfferErrors.InventoryMismatch(inventory.Id.Value, InventoryId.Value));
        }
    }

    public void Activate()
    {
        if (Status != OfferStatus.Draft)
        {
            throw new DomainRuleViolationException(OfferErrors.CanOnlyActivateOfferInDraftStatus());
        }

        if (_priceZones.Count == 0)
        {
            throw new DomainRuleViolationException(OfferErrors.OfferMustHaveAtLeastOnePriceZoneToActivate());
        }

        foreach (var priceZone in _priceZones)
        {
            if (!priceZone.HasItems)
            {
                throw new DomainRuleViolationException(OfferErrors.PriceZoneMustHaveAtLeastOneItem());
            }
        }

        Status = OfferStatus.Active;
    }

    private void EnsureDraft()
    {
        if (Status != OfferStatus.Draft)
        {
            throw new DomainRuleViolationException(OfferErrors.CanOnlySetPriceZonesInDraftStatus());
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
