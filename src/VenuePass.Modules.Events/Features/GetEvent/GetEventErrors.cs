using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Events.Features.GetEvent;

public static class GetEventErrors
{
    public static Error EventNotFound(Guid eventId) => Error.NotFound(
        code: "Events.GetEvent.EventNotFound",
        message: $"Event with ID '{eventId}' was not found.");
}
