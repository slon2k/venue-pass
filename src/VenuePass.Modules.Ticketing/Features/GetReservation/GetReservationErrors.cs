using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.GetReservation;

public static class GetReservationErrors
{
    public static Error ReservationNotFound(Guid reservationId) => Error.NotFound(
        code: "ReservationNotFound",
        message: $"No reservation found with ID '{reservationId}'.");

    public static Error OfferNotFound(Guid offerId) => Error.NotFound(
        code: "OfferNotFound",
        message: $"No offer found with ID '{offerId}'.");
}