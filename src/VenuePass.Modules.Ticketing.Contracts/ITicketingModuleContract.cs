namespace VenuePass.Modules.Ticketing.Contracts;

public interface ITicketingModuleContract
{
    Task<TicketValidationResultDto> ValidateTicketForPublishedEventReferenceAsync(
        string ticketCode,
        Guid publishedEventReferenceId,
        CancellationToken cancellationToken = default);
}