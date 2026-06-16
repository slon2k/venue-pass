namespace VenuePass.Modules.Ticketing.Domain.Tickets;

public interface ITicketCodeGenerator
{
    TicketCode Generate();
}