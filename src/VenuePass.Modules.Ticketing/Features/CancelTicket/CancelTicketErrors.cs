using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Ticketing.Domain.Tickets;

namespace VenuePass.Modules.Ticketing.Features.CancelTicket;

public static class CancelTicketErrors
{
    public static Error TicketNotFound(TicketId ticketId) => Error.NotFound(
        "Ticketing.CancelTicket.TicketNotFound",
        $"No ticket found with ID {ticketId}.");

    public static Error ConcurrencyConflict() => Error.Conflict(
        "Ticketing.CancelTicket.ConcurrencyConflict",
        "A concurrency conflict occurred while cancelling the ticket.");
}