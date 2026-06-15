using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Reservations;

namespace VenuePass.Modules.Ticketing.Domain.Orders;

public static class OrderErrors
{
    public static DomainError ReservationNotActive(ReservationId reservationId) => new(
        "Ticketing.Orders.ReservationNotActive",
        $"Reservation with ID '{reservationId.Value}' is not active.");

}