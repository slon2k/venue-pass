using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.GetReservation;

public static class GetReservationErrors
{
    public static Error ReservationNotFound(Guid reservationId) => Error.NotFound(
        code: "Ticketing.GetReservation.ReservationNotFound",
        message: $"No reservation found with ID '{reservationId}'.");
}