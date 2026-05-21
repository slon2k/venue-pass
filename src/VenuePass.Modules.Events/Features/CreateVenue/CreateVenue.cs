using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Infrastructure;

namespace VenuePass.Modules.Events.Features.CreateVenue;

public sealed class CreateVenueHandler(EventsDbContext db, ILogger<CreateVenueHandler> logger)
{
    public async Task<Result<CreateVenueResult>> Handle(CreateVenueCommand command, CancellationToken ct)
    {
        try
        {
            Venue venue = ToEntity(command);

            if (await db.Venues.AnyAsync(v => v.Name == venue.Name && v.Address.City == venue.Address.City, ct))
            {
                logger.LogWarning(
                    "Venue with name {VenueName} already exists in city {City}.",
                    venue.Name,
                    venue.Address.City);

                return Error.Conflict(
                    "Venue.Create.Duplicate",
                    $"A venue with the name '{venue.Name}' already exists in city '{venue.Address.City}'.");
            }

            db.Venues.Add(venue);
            await db.SaveChangesAsync(ct);

            return ToResult(venue);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid venue data.");
            return Error.Validation("Venue.Create.Validation", ex.Message);
        }
    }

    private static Venue ToEntity(CreateVenueCommand command)
    {
        return Venue.Create(
            new VenueName(command.Name),
            new VenueAddress(
                new StreetAddress(command.StreetAddress),
                new City(command.City),
                new Country(command.Country)),
            new VenueCapacity(command.Capacity));
    }

    private static CreateVenueResult ToResult(Venue venue) => new(
        venue.Id,
        venue.Name,
        venue.Address.StreetAddress,
        venue.Address.City,
        venue.Address.Country,
        venue.Capacity);
}

public sealed record CreateVenueCommand(
    string Name,
    string StreetAddress,
    string City,
    string Country,
    int Capacity);

public sealed record CreateVenueResult(
    Guid VenueId,
    string Name,
    string StreetAddress,
    string City,
    string Country,
    int Capacity);