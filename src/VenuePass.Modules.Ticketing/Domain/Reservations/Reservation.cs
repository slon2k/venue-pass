using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;

namespace VenuePass.Modules.Ticketing.Domain.Reservations;

public class Reservation : AggregateRoot<ReservationId>
{
    private readonly List<ReservationItem> _items = [];

    public OfferId OfferId { get; private set; }
    
    public InventoryId InventoryId { get; private set; }

    public ReservationStatus Status { get; private set; } = ReservationStatus.Reserved;

    public DateTimeOffset ExpiresAt { get; private set; }

    public Currency Currency { get; private set; } = null!;

    public Amount Total { get; private set; } = new Amount(0);

    public IReadOnlyList<ReservationItem> Items => _items.AsReadOnly();

    private Reservation() { }

    private Reservation(ReservationId id, OfferId offerId, InventoryId inventoryId, DateTimeOffset expiresAt, Currency currency) : base(id)
    {
        OfferId = offerId;
        InventoryId = inventoryId;
        ExpiresAt = expiresAt;
        Currency = currency;
    }

    public static Reservation Create(Offer offer, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(offer);

        if (offer.Status != OfferStatus.Active)
        {
            throw new DomainRuleViolationException(
                ReservationErrors.OfferMustBeActiveToCreateReservation());
        }

        if (!offer.SalesRange.Contains(now))
        {
            throw new DomainRuleViolationException(
                ReservationErrors.OfferNotOnSale());
        }

        if (expiresAt <= now)
        {
            throw new DomainRuleViolationException(
                ReservationErrors.ExpirationTimeMustBeInTheFuture());
        }

        return new Reservation(
            id: ReservationId.Create(),
            offerId: offer.Id,
            inventoryId: offer.InventoryId,
            expiresAt: expiresAt,
            currency: offer.Currency
        );
    }

    public void AddSeat(Offer offer, InventorySeatId inventorySeatId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(offer);

        if (offer.Id != OfferId)
        {
            throw new ArgumentException(
                "The provided offer does not match the reservation's offer.",
                nameof(offer));
        }

        EnsureReservedStatus();
        EnsureNotExpired(now);

        if (_items.Any(item => item.InventorySeatId == inventorySeatId))
        {
            throw new DomainRuleViolationException(
                ReservationErrors.DuplicateSeatInReservation(inventorySeatId));
        }
 
        var reservationItem = ReservationItem.CreateForSeat(offer, inventorySeatId);
        _items.Add(reservationItem);
        RecalculateTotal();
    }

    public void AddGeneralAdmission(Offer offer, GeneralAdmissionPoolId poolId, Quantity quantity, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(offer);

        if (offer.Id != OfferId)
        {
            throw new ArgumentException(
                "The provided offer does not match the reservation's offer.",
                nameof(offer));
        }

        EnsureReservedStatus();
        EnsureNotExpired(now);

        if (_items.Any(item => item.GeneralAdmissionPoolId == poolId))
        {
            throw new DomainRuleViolationException(
                ReservationErrors.DuplicateGeneralAdmissionPoolInReservation(poolId));
        }
        var reservationItem = ReservationItem.CreateForGeneralAdmission(offer, poolId, quantity);
        _items.Add(reservationItem);
        RecalculateTotal();
    }

    public void Cancel()
    {
        EnsureReservedStatus();
        Status = ReservationStatus.Cancelled;
    }

    public void Expire(DateTimeOffset now)
    {
        EnsureReservedStatus();
        if (ExpiresAt > now)
        {
            throw new DomainRuleViolationException(
                ReservationErrors.ReservationNotExpiredYet(Id));
        }
        Status = ReservationStatus.Expired;
    }

    public void Complete(DateTimeOffset now)
    {
        EnsureReservedStatus();
        EnsureNotExpired(now);
        EnsureHasItems();
        Status = ReservationStatus.Completed;
    }

    private void RecalculateTotal()
    {
        Total = new Amount(_items.Sum(item => item.Total.Value));
    }

    private void EnsureReservedStatus()
    {
        if (Status != ReservationStatus.Reserved)
        {
            throw new DomainRuleViolationException(
                ReservationErrors.ReservationIsNotInReservedStatus(Id));
        }
    }

    private void EnsureNotExpired(DateTimeOffset now)
    {
        if (ExpiresAt <= now)
        {
            throw new DomainRuleViolationException(
                ReservationErrors.ReservationAlreadyExpired(Id));
        }
    }

    private void EnsureHasItems()
    {
        if (_items.Count == 0)
        {
            throw new DomainRuleViolationException(
                ReservationErrors.ReservationMustHaveItems(Id));
        }
    }
}

public readonly record struct ReservationId(Guid Value)
{
    public static ReservationId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(ReservationId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}

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
        }

        Type = type;
        PriceZoneId = priceZoneId;
        InventorySeatId = inventorySeatId;
        GeneralAdmissionPoolId = generalAdmissionPoolId;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Total = new Amount(unitPrice.Value * quantity.Value);
    }

    internal static ReservationItem CreateForSeat(Offer offer, InventorySeatId inventorySeatId)
    {
        ArgumentNullException.ThrowIfNull(offer);

        foreach (var priceZone in offer.PriceZones)
        {
            if (priceZone.InventorySeatItems.Any(i => i.InventorySeatId == inventorySeatId))
            {
                return new ReservationItem(
                    id: ReservationItemId.Create(),
                    type: ReservationItemType.Seat,
                    priceZoneId: priceZone.Id,
                    inventorySeatId: inventorySeatId,
                    generalAdmissionPoolId: null,
                    unitPrice:  priceZone.Price,
                    quantity: new Quantity(1)
                );
            }
        }

        throw new DomainRuleViolationException(
            ReservationErrors.SeatNotCoveredByOffer(inventorySeatId));
    }

    internal static ReservationItem CreateForGeneralAdmission(Offer offer, GeneralAdmissionPoolId generalAdmissionPoolId, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(offer);

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

public enum ReservationStatus
{
    Reserved = 1,
    Cancelled = 2,
    Expired = 3,
    Completed = 4
}

public enum ReservationItemType
{
    Seat = 1,
    GeneralAdmissionPool = 2
}
