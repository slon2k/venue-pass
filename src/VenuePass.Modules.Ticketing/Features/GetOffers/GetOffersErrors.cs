using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.GetOffers;

public static class GetOffersErrors
{
    public static Error EventNotFound(Guid eventId) => Error.NotFound(
        "Ticketing.GetOffers.EventNotFound",
        $"Event '{eventId}' has not been published or no inventory exists.");
}
