namespace VenuePass.Modules.Ticketing.Contracts;

public interface ITicketingModuleContract
{
    Task<TicketValidationResultDto> ValidateTicketForEventAsync(
        string ticketCode,
        Guid eventId,
        CancellationToken cancellationToken = default);
}