using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.GetOffer;

public static class GetOfferErrors
{
    public static Error OfferNotFound(Guid offerId) => Error.NotFound(
        "Ticketing.GetOffer.OfferNotFound",
        $"Offer '{offerId}' was not found.");
}
