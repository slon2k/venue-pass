using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Events.Features.CreateManifestTemplate;

public static class CreateManifestTemplateErrors
{
    public static Error VenueNotFound(Guid venueId) => Error.NotFound(
        code: "Events.CreateManifestTemplate.VenueNotFound",
        message: $"Venue with ID '{venueId}' was not found.");

    public static ValidationError InvalidData(
        IReadOnlyList<ValidationErrorDetail> details) =>
        ValidationError.Create(
            code: "Events.CreateManifestTemplate.InvalidData",
            message: "Invalid manifest template data.",
            details: details);

    public static ValidationError InvalidData(string message) =>
        ValidationError.Create(
            code: "Events.CreateManifestTemplate.InvalidData",
            message: "Invalid manifest template data.",
            details: [new ValidationErrorDetail(string.Empty, message)]);
}
