using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.ActivateOffer;

public static class ActivateOfferErrors
{
    public static Error OfferNotFound(Guid offerId) => Error.NotFound(
        "Ticketing.ActivateOffer.OfferNotFound",
        $"Offer '{offerId}' was not found.");

    public static ValidationError InvalidData(IReadOnlyList<ValidationErrorDetail> details) =>
        ValidationError.Create("Ticketing.ActivateOffer.InvalidData", "Cannot activate offer.", details);

    public static ValidationError InvalidData(string message) =>
        ValidationError.Create("Ticketing.ActivateOffer.InvalidData", "Cannot activate offer.",
            [new ValidationErrorDetail(string.Empty, message)]);
}
