using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;

namespace VenuePass.Modules.Ticketing.Domain.Reservations;

public class ReservationItem : Entity<ReservationItemId>
{
    public ReservationItemType Type { get; private set; }

    public PriceZoneId PriceZoneId { get; private set; }
    
    public InventorySeatId? InventorySeatId { get; private set; }

    public GeneralAdmissionPoolId? GeneralAdmissionPoolId { get; private set; }

    public Amount UnitPrice { get; private set; }

    public Quantity Quantity { get; private set; }

    public Amount Total { get; private set; }

    private ReservationItem() { }

    private ReservationItem(ReservationItemId id, ReservationItemType type, PriceZoneId priceZoneId, InventorySeatId? inventorySeatId, GeneralAdmissionPoolId? generalAdmissionPoolId, Amount unitPrice, Quantity quantity) : base(id)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Reservation item ID cannot be empty.", nameof(id));
        }

        if (priceZoneId.IsEmpty)
        {
            throw new ArgumentException("Price zone ID cannot be empty.", nameof(priceZoneId));
        }

        if (type != ReservationItemType.Seat && type != ReservationItemType.GeneralAdmissionPool)
        {
            throw new ArgumentException("Invalid reservation item type.", nameof(type));
        }

        if (type == ReservationItemType.Seat)
        {
            if (inventorySeatId is null || inventorySeatId.Value.IsEmpty)
            {
                throw new ArgumentException("Inventory seat ID is required for seat reservation item.");
            }

            if (generalAdmissionPoolId is not null)
            {
                throw new ArgumentException("Seat reservation item cannot have a general admission pool ID.");
            }

            if (quantity.Value != 1)
            {
                throw new ArgumentException("Seat reservation item quantity must be 1.", nameof(quantity));
            }
        }

        if (type == ReservationItemType.GeneralAdmissionPool)
        {
            if (generalAdmissionPoolId is null || generalAdmissionPoolId.Value.IsEmpty)
            {
                throw new ArgumentException("General admission pool ID is required for GA reservation item.");
            }

            if (inventorySeatId is not null)
            {
                throw new ArgumentException("GA reservation item cannot have an inventory seat ID.");
            }

            if (quantity.Value <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
            }
        }

        Type = type;
        PriceZoneId = priceZoneId;
        InventorySeatId = inventorySeatId;
        GeneralAdmissionPoolId = generalAdmissionPoolId;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Total = new Amount(unitPrice.Value * quantity.Value);
    }

    internal static ReservationItem Create(Offer offer, ReservationItemInventorySeatInput inventorySeatInput)
    {
        ArgumentNullException.ThrowIfNull(offer);

        foreach (var priceZone in offer.PriceZones)
        {
            if (priceZone.InventorySeatItems.Any(i => i.InventorySeatId == inventorySeatInput.InventorySeatId))
            {
                return new ReservationItem(
                    id: ReservationItemId.Create(),
                    type: ReservationItemType.Seat,
                    priceZoneId: priceZone.Id,
                    inventorySeatId: inventorySeatInput.InventorySeatId,
                    generalAdmissionPoolId: null,
                    unitPrice:  priceZone.Price,
                    quantity: new Quantity(1)
                );
            }
        }

        throw new DomainRuleViolationException(
            ReservationErrors.SeatNotCoveredByOffer(inventorySeatInput.InventorySeatId));
    }

    internal static ReservationItem Create(Offer offer, ReservationItemGeneralAdmissionPoolInput generalAdmissionPoolInput)
    {
        ArgumentNullException.ThrowIfNull(offer);

        var generalAdmissionPoolId = generalAdmissionPoolInput.GeneralAdmissionPoolId;
        var quantity = generalAdmissionPoolInput.Quantity;

        foreach (var priceZone in offer.PriceZones)
        {
            if (priceZone.GeneralAdmissionPoolItems.Any(i => i.GeneralAdmissionPoolId == generalAdmissionPoolId))
            {
                return new ReservationItem(
                    id: ReservationItemId.Create(),
                    type: ReservationItemType.GeneralAdmissionPool,
                    priceZoneId: priceZone.Id,
                    inventorySeatId: null,
                    generalAdmissionPoolId: generalAdmissionPoolId,
                    unitPrice: priceZone.Price,
                    quantity: quantity
                );
            }
         }

        throw new DomainRuleViolationException(
            ReservationErrors.GeneralAdmissionPoolNotCoveredByOffer(generalAdmissionPoolId));
    }  
}

public readonly record struct ReservationItemId(Guid Value)
{
    public static ReservationItemId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(ReservationItemId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}

public enum ReservationItemType
{
    Seat = 1,
    GeneralAdmissionPool = 2
}

public readonly record struct ReservationItemGeneralAdmissionPoolInput
{
    public GeneralAdmissionPoolId GeneralAdmissionPoolId { get; init; }
    public Quantity Quantity { get; init; }

    public ReservationItemGeneralAdmissionPoolInput(GeneralAdmissionPoolId generalAdmissionPoolId, Quantity quantity)
    {
        if (generalAdmissionPoolId.IsEmpty)
        {
            throw new ArgumentException("General admission pool ID cannot be empty.", nameof(generalAdmissionPoolId));
        }
        GeneralAdmissionPoolId = generalAdmissionPoolId;
        Quantity = quantity;
    }
}

public readonly record struct ReservationItemInventorySeatInput
{
    public InventorySeatId InventorySeatId { get; init; }

    public ReservationItemInventorySeatInput(InventorySeatId inventorySeatId)
    {
        if (inventorySeatId.IsEmpty)
        {
            throw new ArgumentException("Inventory seat ID cannot be empty.", nameof(inventorySeatId));
        }
        InventorySeatId = inventorySeatId;
    }
}

