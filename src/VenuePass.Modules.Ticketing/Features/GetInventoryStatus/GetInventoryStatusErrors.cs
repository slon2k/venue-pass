using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.GetInventoryStatus;

public static class GetInventoryStatusErrors
{
    public static Error EventNotFound(Guid eventId) => Error.NotFound(
        "Ticketing.GetInventoryStatus.EventNotFound",
        $"Event '{eventId}' has not been published or no inventory exists.");
}
