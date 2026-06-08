using VenuePass.BuildingBlocks.Domain;
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
}

public readonly record struct InventoryId(Guid Value)
{
    public static InventoryId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(InventoryId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}

