using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Infrastructure;

namespace VenuePass.Modules.Events.Features.CreateVenue;

public sealed class CreateVenueHandler(
    EventsDbContext db,
    IValidator<CreateVenueCommand> validator,
    ILogger<CreateVenueHandler> logger)
{
    public async Task<Result<CreateVenueResult>> Handle(
        CreateVenueCommand command,
        CancellationToken ct)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            return CreateVenueErrors.InvalidData(
                [.. validationResult.Errors.Select(e =>
                    new ValidationErrorDetail(e.PropertyName, e.ErrorMessage))]);
        }

        Venue venue;

        try
        {
            venue = ToEntity(command);
        }
        catch (ArgumentException ex)
        {
            logger.LogInformation(ex, "Domain validation rejected venue creation.");
            return CreateVenueErrors.InvalidData(ex.Message);
        }

        if (await db.Venues.AnyAsync(v => v.Name == venue.Name && v.Address.City == venue.Address.City, ct))
        {
            return CreateVenueErrors.VenueAlreadyExists(venue.Name, venue.Address.City);
        }

        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        return ToResult(venue);        
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