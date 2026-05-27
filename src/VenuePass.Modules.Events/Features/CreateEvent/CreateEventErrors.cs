using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Events.Features.CreateEvent;

public static class CreateEventErrors
{
    public static Error VenueNotFound(Guid venueId) => Error.NotFound(
        code: "Events.CreateEvent.VenueNotFound",
        message: $"Venue with ID '{venueId}' was not found.");

    public static Error ManifestTemplateNotFound(Guid templateId) => Error.NotFound(
        code: "Events.CreateEvent.ManifestTemplateNotFound",
        message: $"Manifest template with ID '{templateId}' was not found.");

    public static Error ManifestTemplateVenueMismatch(Guid templateId, Guid venueId) => Error.Conflict(
        code: "Events.CreateEvent.ManifestTemplateVenueMismatch",
        message: $"Manifest template '{templateId}' does not belong to venue '{venueId}'.");

    public static ValidationError InvalidData(
        IReadOnlyList<ValidationErrorDetail> details) =>
        ValidationError.Create(
            code: "Events.CreateEvent.InvalidData",
            message: "Invalid event data.",
            details: details);

    public static ValidationError InvalidData(string message) =>
        ValidationError.Create(
            code: "Events.CreateEvent.InvalidData",
            message: "Invalid event data.",
            details: [new ValidationErrorDetail(string.Empty, message)]);
}
