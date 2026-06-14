using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.CancelReservation;

public static class CancelReservationErrors
{
    public static Error ReservationNotFound(Guid reservationId) => Error.NotFound(
        "Ticketing.CancelReservation.ReservationNotFound",
        $"No reservation found with ID {reservationId}.");

    public static Error ConcurrencyConflict() => Error.Concurrency(
        "Ticketing.CancelReservation.ConcurrencyConflict",
        "The reservation could not be cancelled due to a concurrency conflict. Please try again.");

    public static Error InventoryNotFound(Guid inventoryId) => Error.Unexpected(
        "Ticketing.CancelReservation.InventoryNotFound",
        $"No inventory found with ID {inventoryId}.");        
}
