using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Inventories;

namespace VenuePass.Modules.Ticketing.Domain.Reservations;

public static class ReservationErrors
{
    public static DomainError OfferMustBeActiveToCreateReservation() => new(
        "Ticketing.Reservation.OfferMustBeActiveToCreateReservation",
        "Offer must be active to create a reservation.");

    public static DomainError ReservationIsNotInReservedStatus(ReservationId reservationId) => new(
        "Ticketing.Reservation.ReservationIsNotInReservedStatus",
        $"Reservation with ID '{reservationId.Value}' is not in reserved status.");

    public static DomainError ReservationAlreadyExpired(ReservationId reservationId) => new(
        "Ticketing.Reservation.ReservationAlreadyExpired",
        $"Reservation with ID '{reservationId.Value}' has already expired.");

    public static DomainError SeatNotCoveredByOffer(InventorySeatId inventorySeatId) => new(
        "Ticketing.Reservation.SeatNotCoveredByOffer",
        $"Seat with ID '{inventorySeatId.Value}' is not covered by the offer.");

    public static DomainError GeneralAdmissionPoolNotCoveredByOffer(GeneralAdmissionPoolId poolId) => new(
        "Ticketing.Reservation.GeneralAdmissionPoolNotCoveredByOffer",
        $"General admission pool with ID '{poolId.Value}' is not covered by the offer.");

    public static DomainError ExpirationTimeMustBeInTheFuture() => new(
        "Ticketing.Reservation.ExpirationTimeMustBeInTheFuture",
        "Expiration time must be in the future.");

    public static DomainError OfferNotOnSale() => new(
        "Ticketing.Reservation.OfferNotOnSale",
        "Offer is not currently on sale.");

    public static DomainError ReservationNotExpiredYet(ReservationId reservationId) => new(
        "Ticketing.Reservation.ReservationNotExpiredYet",
        $"Reservation with ID '{reservationId.Value}' has not expired yet.");

    public static DomainError DuplicateSeatInReservation(InventorySeatId inventorySeatId) => new(
        "Ticketing.Reservation.DuplicateSeatInReservation",
        $"Seat with ID '{inventorySeatId.Value}' is already added to the reservation.");

    public static DomainError DuplicateGeneralAdmissionPoolInReservation(GeneralAdmissionPoolId poolId) => new(
        "Ticketing.Reservation.DuplicateGeneralAdmissionPoolInReservation",
        $"General admission pool with ID '{poolId.Value}' is already added to the reservation.");

    public static DomainError ReservationMustHaveItems(ReservationId reservationId) => new(
        "Ticketing.Reservation.ReservationMustHaveItems",
        $"Reservation with ID '{reservationId.Value}' must have at least one item to be completed.");
}