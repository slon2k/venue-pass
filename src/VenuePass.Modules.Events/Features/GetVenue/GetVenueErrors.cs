using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Events.Features.GetVenue;

public static class GetVenueErrors
{
    public static Error VenueNotFound(Guid venueId) => Error.NotFound(
        code: "Events.GetVenue.VenueNotFound",
        message: $"Venue with ID '{venueId}' was not found.");
}
