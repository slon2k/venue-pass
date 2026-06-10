using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.GetInventoryStatus;

public sealed class GetInventoryStatusHandler(TicketingDbContext db)
{
    public async Task<Result<GetInventoryStatusResult>> Handle(
        GetInventoryStatusQuery query,
        CancellationToken ct)
    {
        var reference = await db.PublishedEventReferences
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EventId == query.EventId, ct);

        if (reference is null)
        {
            return GetInventoryStatusErrors.EventNotFound(query.EventId);
        }

        var inventory = await db.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.EventReferenceId == reference.Id, ct);

        if (inventory is null)
        {
            return GetInventoryStatusErrors.EventNotFound(query.EventId);
        }

        return ToResult(query.EventId, inventory);
    }

    private static GetInventoryStatusResult ToResult(Guid eventId, Inventory inventory)
    {
        var sections = inventory.Seats
            .GroupBy(s => s.Section.Value)
            .Select(g => new SectionStatusResult(
                g.Key,
                g.Count(),
                g.Count(s => s.Availability == SeatAvailability.Available)))
            .ToList();

        var pools = inventory.Pools
            .Select(p => new PoolStatusResult(p.Name.Value, p.Capacity.Value, p.AvailableCount))
            .ToList();

        return new GetInventoryStatusResult(
            EventId: eventId,
            InventoryId: inventory.Id.Value,
            TotalSeats: inventory.Seats.Count,
            AvailableSeats: inventory.Seats.Count(s => s.Availability == SeatAvailability.Available),
            Sections: sections,
            Pools: pools);
    }
}

public sealed record GetInventoryStatusQuery(Guid EventId);

public sealed record GetInventoryStatusResult(
    Guid EventId,
    Guid InventoryId,
    int TotalSeats,
    int AvailableSeats,
    IReadOnlyList<SectionStatusResult> Sections,
    IReadOnlyList<PoolStatusResult> Pools);

public sealed record SectionStatusResult(
    string Name,
    int TotalSeats,
    int AvailableSeats);

public sealed record PoolStatusResult(
    string Name,
    int TotalCapacity,
    int AvailableCount);
