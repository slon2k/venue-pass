using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Events.Contracts;

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
        ManifestExportDto manifest)
    {
        var inventory = new Inventory(
            InventoryId.Create(),
            eventReferenceId
        );

        foreach (var section in manifest.Sections)
            foreach (var row in section.Rows)
                foreach (var seat in row.Seats)
                    inventory._seats.Add(InventorySeat.Create(
                        sourceSeatId: seat.SeatId,
                        sectionName: section.Name,
                        rowLabel: row.Label,
                        seatLabel: seat.Label));

        foreach (var area in manifest.GeneralAdmissionAreas)
            inventory._pools.Add(GeneralAdmissionPool.Create(
                sourceAreaId: area.AreaId,
                name: area.Name,
                capacity: area.Capacity));

        return inventory;
    }
}

public record InventoryId(Guid Value)
{
    public static InventoryId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(InventoryId id) => id.Value;
    public override string ToString() => Value.ToString();
}

