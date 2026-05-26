using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Events.Features.GetManifestTemplate;

public static class GetManifestTemplateErrors
{
    public static Error ManifestTemplateNotFound(Guid manifestTemplateId) => Error.NotFound(
        code: "Events.GetManifestTemplate.ManifestTemplateNotFound",
        message: $"Manifest template with ID '{manifestTemplateId}' was not found.");
}
