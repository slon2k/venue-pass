using Microsoft.EntityFrameworkCore;
using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Events.Infrastructure;

namespace VenuePass.Modules.Events.Features.GetVenue;

public sealed class GetVenueHandler(EventsDbContext db)
{
    public async Task<Result<GetVenueResult>> Handle(
        GetVenueQuery query,
        CancellationToken ct)
    {
        var venue = await db.Venues
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == query.VenueId, ct);

        if (venue is null)
        {
            return GetVenueErrors.VenueNotFound(query.VenueId);
        }

        return ToResult(venue);
    }

    private static GetVenueResult ToResult(Domain.Venues.Venue venue) => new(
        venue.Id,
        venue.Name,
        venue.Address.StreetAddress,
        venue.Address.City,
        venue.Address.Country,
        venue.Capacity);
}

public sealed record GetVenueQuery(Guid VenueId);

public sealed record GetVenueResult(
    Guid VenueId,
    string Name,
    string StreetAddress,
    string City,
    string Country,
    int Capacity);
