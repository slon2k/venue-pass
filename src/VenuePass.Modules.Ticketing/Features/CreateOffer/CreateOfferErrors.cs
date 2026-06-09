using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.CreateOffer;

public static class CreateOfferErrors
{
    public static Error EventNotPublished(Guid eventId) => Error.NotFound(
        "Ticketing.CreateOffer.EventNotPublished",
        $"Event '{eventId}' has not been published or no inventory exists.");

    public static Error InventoryNotFound(Guid eventId) => Error.NotFound(
        "Ticketing.CreateOffer.InventoryNotFound",
        $"No inventory found for event '{eventId}'.");

    public static ValidationError InvalidData(IReadOnlyList<ValidationErrorDetail> details) =>
        ValidationError.Create("Ticketing.CreateOffer.InvalidData", "Invalid offer data.", details);

    public static ValidationError InvalidData(string message) =>
        ValidationError.Create("Ticketing.CreateOffer.InvalidData", "Invalid offer data.",
            [new ValidationErrorDetail(string.Empty, message)]);
}
