using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Events.Features.ReassignEventManager;

public static class ReassignEventManagerErrors
{
    public static Error EventNotFound(Guid eventId) => Error.NotFound(
        code: "Events.ReassignEventManager.EventNotFound",
        message: $"Event with ID '{eventId}' was not found.");

    public static ValidationError InvalidData(
        IReadOnlyList<ValidationErrorDetail> details) =>
        ValidationError.Create(
            code: "Events.ReassignEventManager.InvalidData",
            message: "Invalid reassign manager data.",
            details: details);
}
