using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;

namespace VenuePass.Modules.Ticketing.Domain.Inventories;

public sealed class Inventory : AggregateRoot<InventoryId>
{
    private readonly List<InventorySeat> _seats = [];
    private readonly List<GeneralAdmissionPool> _pools = [];

    public PublishedEventReferenceId EventReferenceId { get; private set; } = null!;
    public IReadOnlyList<InventorySeat> Seats => _seats;
    public IReadOnlyList<GeneralAdmissionPool> Pools => _pools;

    private Inventory() { }

    private Inventory(InventoryId id, PublishedEventReferenceId eventReferenceId) : base(id)
    {
        EventReferenceId = eventReferenceId;
    }

    public static Inventory CreateFromManifest(
        PublishedEventReferenceId eventReferenceId,
        InventoryManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var inventory = new Inventory(
            InventoryId.Create(),
            eventReferenceId
        );

        var sourceSeatIds = manifest.Sections
            .SelectMany(section => section.Rows)
            .SelectMany(row => row.Seats)
            .Select(seat => seat.SeatId)
            .ToList();

        if (sourceSeatIds.Count != sourceSeatIds.Distinct().Count())
        {
            throw new DomainRuleViolationException(InventoryErrors.DuplicateSourceSeats());
        }

        var sourceAreaIds = manifest.GeneralAdmissionAreas
            .Select(area => area.AreaId)
            .ToList();

        if (sourceAreaIds.Count != sourceAreaIds.Distinct().Count())
        {
            throw new DomainRuleViolationException(InventoryErrors.DuplicateSourceGeneralAdmissionAreas());
        }

        foreach (var section in manifest.Sections)
        {
            foreach (var row in section.Rows)
            {
                foreach (var seat in row.Seats)
                {
                    inventory._seats.Add(
                        InventorySeat.Create(
                            seat.SeatId,
                            section.Name,
                            row.Label,
                            seat.Label));
                }
            }
        }

        foreach (var area in manifest.GeneralAdmissionAreas)
        {
            inventory._pools.Add(
                GeneralAdmissionPool.Create(
                    area.AreaId,
                    area.Name,
                    area.Capacity));
        }

        if (inventory._seats.Count == 0 && inventory._pools.Count == 0)
        {
            throw new DomainRuleViolationException(InventoryErrors.MustContainInventoryItems());
        }

        return inventory;
    }

    public void ReserveSeats(IReadOnlyList<InventorySeatId> seatIds)
    {
        ArgumentNullException.ThrowIfNull(seatIds);
    
        if (seatIds.Count == 0)
        {
            throw new ArgumentException("Seat ID list cannot be empty.", nameof(seatIds));
        }

        var inventorySeats = GetSeatsOrThrow(seatIds);

        EnsureSeatsAvailable(inventorySeats);

        foreach (var seat in inventorySeats)
        {
            seat.Reserve();
        }
    }

    public void ReserveGeneralAdmissionPool(GeneralAdmissionPoolId poolId, Quantity quantity)
    {
        var pool = _pools.FirstOrDefault(p => p.Id == poolId) 
            ?? throw new DomainRuleViolationException(
                InventoryErrors.GeneralAdmissionPoolNotFound(poolId));

        pool.Reserve(quantity);
    }

    public void ReleaseSeats(IReadOnlyList<InventorySeatId> seatIds)
    {
        ArgumentNullException.ThrowIfNull(seatIds);

        if (seatIds.Count == 0)
        {
            throw new ArgumentException("Seat ID list cannot be empty.", nameof(seatIds));
        }

        var inventorySeats = GetSeatsOrThrow(seatIds);

        EnsureSeatsReserved(inventorySeats);

        foreach (var seat in inventorySeats)
        {
            seat.Release();
        }
    }

    public void ReleaseGeneralAdmissionPool(GeneralAdmissionPoolId poolId, Quantity quantity)
    {
        var pool = _pools.FirstOrDefault(p => p.Id == poolId) 
            ?? throw new DomainRuleViolationException(
                InventoryErrors.GeneralAdmissionPoolNotFound(poolId));

        pool.Release(quantity);
    }

    public void SellSeats(IReadOnlyList<InventorySeatId> seatIds)
    {
        ArgumentNullException.ThrowIfNull(seatIds);

        if (seatIds.Count == 0)
        {
            throw new ArgumentException("Seat ID list cannot be empty.", nameof(seatIds));
        }

        var inventorySeats = GetSeatsOrThrow(seatIds);

        EnsureSeatsReserved(inventorySeats);

        foreach (var seat in inventorySeats)
        {
            seat.Sell();
        }
    }

    public void SellGeneralAdmissionPool(GeneralAdmissionPoolId poolId, Quantity quantity)
    {
        var pool = _pools.FirstOrDefault(p => p.Id == poolId) 
            ?? throw new DomainRuleViolationException(
                InventoryErrors.GeneralAdmissionPoolNotFound(poolId));

        pool.Sell(quantity);
    }

    private List<InventorySeat> GetSeatsOrThrow(IReadOnlyList<InventorySeatId> seatIds)
    {
        var seatSet = new HashSet<InventorySeatId>(seatIds);

        if (seatSet.Count != seatIds.Count)
        {
            throw new ArgumentException(
                "Seat ID list cannot contain duplicate items.",
                nameof(seatIds));
        }

        var inventorySeats = _seats
            .Where(seat => seatSet.Contains(seat.Id))
            .ToList();

        if (inventorySeats.Count != seatSet.Count)
        {
            throw new DomainRuleViolationException(
                InventoryErrors.SomeSeatsNotFound(seatIds));
        }

        return inventorySeats;
    }

    private static void EnsureSeatsAvailable(List<InventorySeat> inventorySeats)
    {
        foreach (var seat in inventorySeats)
        {
            if (!seat.IsAvailable)
            {
                throw new DomainConflictException(
                    InventoryErrors.SeatNotAvailable(seat.Id));
            }
        }
    }

    private static void EnsureSeatsReserved(List<InventorySeat> inventorySeats)
    {
        foreach (var seat in inventorySeats)
        {
            if (!seat.IsReserved)
            {
                throw new DomainConflictException(
                    InventoryErrors.SeatNotReserved(seat.Id));
            }
        }
    }
}

public readonly record struct InventoryId(Guid Value)
{
    public static InventoryId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(InventoryId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}

