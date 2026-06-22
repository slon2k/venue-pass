using Microsoft.EntityFrameworkCore;

using VenuePass.Modules.Ticketing.Contracts;
using VenuePass.Modules.Ticketing.Domain.Tickets;

namespace VenuePass.Modules.Ticketing.Infrastructure;

public sealed class TicketModuleContract(TicketingDbContext db) : ITicketingModuleContract
{

    public async Task<TicketValidationResultDto> ValidateTicketForEventAsync(
        string ticketCode,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        if (!await db.PublishedEventReferences.AnyAsync(e => e.EventId == eventId, cancellationToken))
        {
            return TicketValidationResultDto.EventNotFound();
        }

        if (!TicketCode.TryCreate(ticketCode, out var validTicketCode))
        {
            return TicketValidationResultDto.MalformedTicketCode();
        }

        var ticketWithEventId = await db.Tickets
            .AsNoTracking()
            .Where(t => t.Code == validTicketCode)
            .Join(db.PublishedEventReferences,
                t => t.PublishedEventReferenceId,
                r => r.Id,
                (t, r) => new { Ticket = t, ExternalEventId = r.EventId })
            .FirstOrDefaultAsync(cancellationToken);

        if (ticketWithEventId is null)
        {
            return TicketValidationResultDto.TicketNotFound();
        }

        var ticketDto = CreateTicketExportDto(ticketWithEventId.Ticket, ticketWithEventId.ExternalEventId);

        return TicketValidationResultDto.CreateForEvent(ticketDto, eventId);
    }

    private static TicketValidationStatus MapTicketStatus(TicketStatus status) => status switch
    {
        TicketStatus.Issued => TicketValidationStatus.Issued,
        TicketStatus.Canceled => TicketValidationStatus.Canceled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), $"Unsupported ticket status: {status}")
    };

    private static TicketExportDto CreateTicketExportDto(Ticket ticket, Guid externalEventId)
    {
        if (ticket.InventorySeatId is not null && ticket.GeneralAdmissionPoolId is not null)
        {
            throw new InvalidOperationException("Invalid ticket association: A ticket cannot be associated with both an inventory seat and a general admission pool.");
        }

        return ticket.InventorySeatId is not null
            ? TicketExportDto.CreateForSeat(
                ticketId: ticket.Id.Value,
                publishedEventReferenceId: externalEventId,
                orderId: ticket.OrderId.Value,
                orderItemId: ticket.OrderItemId.Value,
                code: ticket.Code.Value,
                inventorySeatId: ticket.InventorySeatId.Value.Value,
                status: MapTicketStatus(ticket.Status),
                issuedAt: ticket.CreatedAt)
            : TicketExportDto.CreateForGeneralAdmission(
                ticketId: ticket.Id.Value,
                publishedEventReferenceId: externalEventId,
                orderId: ticket.OrderId.Value,
                orderItemId: ticket.OrderItemId.Value,
                code: ticket.Code.Value,
                generalAdmissionPoolId: ticket.GeneralAdmissionPoolId?.Value ?? throw new InvalidOperationException("GeneralAdmissionPoolId must have a value for general admission tickets."),
                status: MapTicketStatus(ticket.Status),
                issuedAt: ticket.CreatedAt);
    }
}