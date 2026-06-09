using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.ConfigurePricing;

public static class ConfigurePricingErrors
{
    public static Error OfferNotFound(Guid offerId) => Error.NotFound(
        "Ticketing.ConfigurePricing.OfferNotFound",
        $"Offer '{offerId}' was not found.");

    public static ValidationError InvalidData(IReadOnlyList<ValidationErrorDetail> details) =>
        ValidationError.Create("Ticketing.ConfigurePricing.InvalidData", "Invalid pricing data.", details);

    public static ValidationError InvalidData(string message) =>
        ValidationError.Create("Ticketing.ConfigurePricing.InvalidData", "Invalid pricing data.",
            [new ValidationErrorDetail(string.Empty, message)]);
}
