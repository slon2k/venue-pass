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

    private Reservation(
        ReservationId id,
        OfferId offerId,
        InventoryId inventoryId,
        IReadOnlyList<ReservationItem> items,
        DateTimeOffset expiresAt,
        Currency currency) : base(id)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Reservation ID cannot be empty.", nameof(id));
        }

        if (offerId.IsEmpty)
        {
            throw new ArgumentException("Offer ID cannot be empty.", nameof(offerId));
        }

        if (inventoryId.IsEmpty)
        {
            throw new ArgumentException("Inventory ID cannot be empty.", nameof(inventoryId));
        }

        if (items.Count == 0)
        {
            throw new DomainRuleViolationException(
                ReservationErrors.ReservationMustHaveItems());
        }

        OfferId = offerId;
        InventoryId = inventoryId;
        ExpiresAt = expiresAt;
        Currency = currency;
        _items.AddRange(items);
        RecalculateTotal();
    }

    public static Reservation Create(
        Offer offer,
        IEnumerable<ReservationItemInventorySeatInput> inventorySeats,
        IEnumerable<ReservationItemGeneralAdmissionPoolInput> generalAdmissionPools,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(offer);

        var seatInputs = inventorySeats?.ToList()
            ?? throw new ArgumentNullException(nameof(inventorySeats));

        var poolInputs = generalAdmissionPools?.ToList()
            ?? throw new ArgumentNullException(nameof(generalAdmissionPools));

        if (seatInputs.Count == 0 && poolInputs.Count == 0)
        {
            throw new DomainRuleViolationException(
                ReservationErrors.ReservationMustHaveItems());
        }

        EnsureOfferActiveAndOnSale(offer, now);
        EnsureExpirationTimeInTheFuture(expiresAt, now);    
        EnsureNoDuplicateSeats(seatInputs);
        EnsureNoDuplicateGeneralAdmissionPools(poolInputs);

        var items = new List<ReservationItem>();

        foreach (var seatInput in seatInputs)
        {
            var reservationItem = ReservationItem.Create(offer, seatInput);
            items.Add(reservationItem);
        }

        foreach (var poolInput in poolInputs)
        {
            var reservationItem = ReservationItem.Create(offer, poolInput);
            items.Add(reservationItem);
        }

        return new Reservation(
            id: ReservationId.Create(),
            offerId: offer.Id,
            inventoryId: offer.InventoryId,
            expiresAt: expiresAt,
            currency: offer.Currency,
            items: items
        );
    }

    private static void EnsureExpirationTimeInTheFuture(DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (expiresAt <= now)
        {
            throw new DomainRuleViolationException(
                ReservationErrors.ExpirationTimeMustBeInTheFuture());
        }
    }

    private static void EnsureOfferActiveAndOnSale(Offer offer, DateTimeOffset now)
    {
        if (offer.Status != OfferStatus.Active)
        {
            throw new DomainConflictException(
                ReservationErrors.OfferMustBeActiveToCreateReservation());
        }

        if (!offer.SalesRange.Contains(now))
        {
            throw new DomainRuleViolationException(
                ReservationErrors.OfferNotOnSale());
        }
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
            throw new DomainConflictException(
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
            throw new DomainConflictException(
                ReservationErrors.ReservationIsNotInReservedStatus(Id));
        }
    }

    private void EnsureNotExpired(DateTimeOffset now)
    {
        if (ExpiresAt <= now)
        {
            throw new DomainConflictException(
                ReservationErrors.ReservationAlreadyExpired(Id));
        }
    }

    private void EnsureHasItems()
    {
        if (_items.Count == 0)
        {
            throw new DomainRuleViolationException(
                ReservationErrors.ReservationMustHaveItems());
        }
    }

    private static void EnsureNoDuplicateSeats(IReadOnlyList<ReservationItemInventorySeatInput> inventorySeats)
    {
        var seatIds = inventorySeats.Select(i => i.InventorySeatId).ToList();
        if (seatIds.Count != seatIds.Distinct().Count())
        {
            throw new DomainRuleViolationException(
                ReservationErrors.DuplicateSeatsInReservation());
        }
    }

    private static void EnsureNoDuplicateGeneralAdmissionPools(IReadOnlyList<ReservationItemGeneralAdmissionPoolInput> generalAdmissionPools)
    {
        var poolIds = generalAdmissionPools.Select(i => i.GeneralAdmissionPoolId).ToList();
        if (poolIds.Count != poolIds.Distinct().Count())
        {
            throw new DomainRuleViolationException(
                ReservationErrors.DuplicateGeneralAdmissionPoolsInReservation());
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


public enum ReservationStatus
{
    Reserved = 1,
    Cancelled = 2,
    Expired = 3,
    Completed = 4
}
