using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Events.Features.PublishEvent;

public static class PublishEventErrors
{
    public static Error EventNotFound(Guid eventId) => Error.NotFound(
        code: "Events.PublishEvent.EventNotFound",
        message: $"Event with ID '{eventId}' was not found.");

    public static Error ManifestNotFound(Guid manifestId) => Error.NotFound(
        code: "Events.PublishEvent.ManifestNotFound",
        message: $"Manifest with ID '{manifestId}' was not found.");

    public static Error CallerIsNotAssignedManager() => Error.Forbidden(
        code: "Events.PublishEvent.CallerIsNotAssignedManager",
        message: "Only the assigned manager may publish this event.");
}
