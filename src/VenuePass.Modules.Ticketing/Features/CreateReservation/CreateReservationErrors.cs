using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.CreateReservation;

public static class CreateReservationErrors
{
    public static Error OfferNotFound(Guid offerId) => Error.NotFound(
        "Ticketing.CreateReservation.OfferNotFound",
        $"Offer '{offerId}' not found.");

    public static Error InventoryNotFound(Guid inventoryId) => Error.NotFound(
        "Ticketing.CreateReservation.InventoryNotFound",
        $"Inventory '{inventoryId}' not found.");

    public static ValidationError InvalidData(IReadOnlyList<ValidationErrorDetail> details) =>
        ValidationError.Create("Ticketing.CreateReservation.InvalidData", "Invalid reservation data.", details);

    public static ValidationError InvalidData(string message) =>
        ValidationError.Create("Ticketing.CreateReservation.InvalidData", "Invalid reservation data.",
            [new ValidationErrorDetail(string.Empty, message)]);

    public static Error ConcurrencyConflict() => Error.Concurrency(
        "Ticketing.CreateReservation.ConcurrencyConflict",
        "The reservation could not be created due to a concurrency conflict. Please try again.");
}