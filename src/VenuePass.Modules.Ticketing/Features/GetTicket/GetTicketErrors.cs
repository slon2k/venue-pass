using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.GetTicket;

public static class GetTicketErrors
{
    public static Error TicketNotFound(string ticketCode) => Error.NotFound(
        "Ticketing.GetTicket.TicketNotFound",
        $"Ticket with code '{ticketCode}' was not found.");
}