using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Ticketing.Contracts;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Domain.Tickets;

namespace VenuePass.Modules.Ticketing.Infrastructure;

public sealed class TicketModuleContract(TicketingDbContext db) : ITicketingModuleContract
{
    public async Task<TicketValidationResultDto> ValidateTicketForPublishedEventReferenceAsync(
        string ticketCode,
        Guid publishedEventReferenceId,
        CancellationToken cancellationToken = default)
    {
        publishedEventReferenceId.ThrowIfEmpty(nameof(publishedEventReferenceId));

        var requestedPublishedEventReferenceId = new PublishedEventReferenceId(publishedEventReferenceId);

        if (!await db.PublishedEventReferences
                .AsNoTracking()
                .AnyAsync(e => e.Id == requestedPublishedEventReferenceId, cancellationToken))
        {
            return TicketValidationResultDto.PublishedEventReferenceNotFound();
        }

        if (!TicketCode.TryCreate(ticketCode, out var validTicketCode))
        {
            return TicketValidationResultDto.MalformedTicketCode();
        }

        var ticket = await db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == validTicketCode, cancellationToken);

        if (ticket is null)
        {
            return TicketValidationResultDto.TicketNotFound();
        }

        var ticketDto = CreateTicketExportDto(ticket);

        return TicketValidationResultDto.CreateForPublishedEventReference(
            ticketDto,
            publishedEventReferenceId);
    }

    private static TicketValidationStatus MapTicketStatus(TicketStatus status) => status switch
    {
        TicketStatus.Issued => TicketValidationStatus.Issued,
        TicketStatus.Canceled => TicketValidationStatus.Canceled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), $"Unsupported ticket status: {status}")
    };

    private static TicketExportDto CreateTicketExportDto(Ticket ticket)
    {
        if (ticket.InventorySeatId is not null && ticket.GeneralAdmissionPoolId is not null)
        {
            throw new InvalidOperationException(
                "Invalid ticket association: A ticket cannot be associated with both an inventory seat and a general admission pool.");
        }

        if (ticket.InventorySeatId is null && ticket.GeneralAdmissionPoolId is null)
        {
            throw new InvalidOperationException(
                "Invalid ticket association: A ticket must be associated with either an inventory seat or a general admission pool.");
        }

        return ticket.InventorySeatId is not null
            ? TicketExportDto.CreateForSeat(
                ticketId: ticket.Id.Value,
                publishedEventReferenceId: ticket.PublishedEventReferenceId.Value,
                orderId: ticket.OrderId.Value,
                orderItemId: ticket.OrderItemId.Value,
                code: ticket.Code.Value,
                inventorySeatId: ticket.InventorySeatId.Value.Value,
                status: MapTicketStatus(ticket.Status),
                issuedAt: ticket.CreatedAt)
            : TicketExportDto.CreateForGeneralAdmission(
                ticketId: ticket.Id.Value,
                publishedEventReferenceId: ticket.PublishedEventReferenceId.Value,
                orderId: ticket.OrderId.Value,
                orderItemId: ticket.OrderItemId.Value,
                code: ticket.Code.Value,
                generalAdmissionPoolId: ticket.GeneralAdmissionPoolId!.Value.Value,
                status: MapTicketStatus(ticket.Status),
                issuedAt: ticket.CreatedAt);
    }
}