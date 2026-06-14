using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.ExpireReservation;

public static class ExpireReservationErrors
{
    public static Error ReservationNotFound(Guid reservationId) => Error.NotFound(
        "Ticketing.ExpireReservation.ReservationNotFound",
        $"No reservation found with ID {reservationId}.");

    public static Error ConcurrencyConflict(Guid reservationId) => Error.Concurrency(
        "Ticketing.ExpireReservation.ConcurrencyConflict",
        $"The reservation with ID {reservationId} could not be expired due to a concurrency conflict. Please try again.");

    public static Error InventoryNotFound(Guid inventoryId) => Error.Unexpected(
        "Ticketing.ExpireReservation.InventoryNotFound",
        $"No inventory found with ID {inventoryId}.");
}