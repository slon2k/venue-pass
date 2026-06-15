using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.CheckoutReservation;

public static class CheckoutReservationErrors
{
    public static ValidationError InvalidData(IReadOnlyList<ValidationErrorDetail> details) =>
        ValidationError.Create(
            "Ticketing.CheckoutReservation.InvalidData",
            "Invalid checkout data.",
            details);

    public static Error ReservationNotFound(Guid reservationId) => Error.NotFound(
        "Ticketing.CheckoutReservation.ReservationNotFound",
        $"No reservation found with ID {reservationId}.");

    public static Error InventoryNotFound(Guid inventoryId) => Error.Unexpected(
        "Ticketing.CheckoutReservation.InventoryNotFound",
        $"No inventory found with ID {inventoryId}.");

    public static Error OrderNotFound(Guid reservationId) => Error.Unexpected(
        "Ticketing.CheckoutReservation.OrderNotFound",
        $"No order found for reservation {reservationId}.");

    public static Error ConcurrencyConflict() => Error.Concurrency(
        "Ticketing.CheckoutReservation.ConcurrencyConflict",
        "The checkout could not be completed due to a concurrency conflict. Please try again.");
}
