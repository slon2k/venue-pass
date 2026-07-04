using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Messaging;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.TicketProjections;
using VenuePass.Modules.Attendance.Infrastructure;
using VenuePass.Modules.Ticketing.Contracts;

namespace VenuePass.Modules.Attendance.Features.TicketIssued;

internal sealed class TicketIssuedHandler(
    AttendanceDbContext db,
    ILogger<TicketIssuedHandler> logger)
    : IIntegrationEventHandler<TicketIssuedIntegrationEvent>
{
    public async Task Handle(
        TicketIssuedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!TicketCode.TryCreate(integrationEvent.TicketCode, out var ticketCode))
        {
            logger.LogError(
                "Received TicketIssued event with malformed ticket code. MessageId: {MessageId}, TicketId: {TicketId}, TicketCode: {TicketCode}",
                integrationEvent.MessageId,
                integrationEvent.TicketId,
                integrationEvent.TicketCode);

            throw new InvalidOperationException(
                $"TicketIssued event contains invalid ticket code '{integrationEvent.TicketCode}'.");
        }

        var ticketId = new TicketId(integrationEvent.TicketId);

        var existingProjections = await LoadExistingProjections(
            ticketId,
            ticketCode,
            cancellationToken);

        if (HandleExistingIfPresent(existingProjections, integrationEvent, ticketCode))
        {
            return;
        }

        var ticketProjection = FromIntegrationEvent(integrationEvent, ticketCode);

        db.TicketProjections.Add(ticketProjection);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateTicketProjection(ex))
        {
            db.ChangeTracker.Clear();

            existingProjections = await LoadExistingProjections(
                ticketId,
                ticketCode,
                cancellationToken);

            if (HandleExistingIfPresent(existingProjections, integrationEvent, ticketCode))
            {
                return;
            }

            throw;
        }
    }

    private async Task<List<TicketProjection>> LoadExistingProjections(
        TicketId ticketId,
        TicketCode ticketCode,
        CancellationToken cancellationToken)
    {
        return await db.TicketProjections
            .Where(projection =>
                projection.Id == ticketId ||
                projection.TicketCode == ticketCode)
            .ToListAsync(cancellationToken);
    }

    private bool HandleExistingIfPresent(
        IReadOnlyList<TicketProjection> existingProjections,
        TicketIssuedIntegrationEvent integrationEvent,
        TicketCode ticketCode)
    {
        if (existingProjections.Count == 0)
        {
            return false;
        }

        var ticketId = new TicketId(integrationEvent.TicketId);

        var byTicketId = existingProjections
            .SingleOrDefault(projection => projection.Id == ticketId);

        var byTicketCode = existingProjections
            .SingleOrDefault(projection => projection.TicketCode == ticketCode);

        if (byTicketId is not null && byTicketId.TicketCode != ticketCode)
        {
            logger.LogError(
                "Ticket projection contract violation. TicketIssued has TicketId {TicketId} with TicketCode {IncomingTicketCode}, but existing projection for same TicketId has TicketCode {ExistingTicketCode}. MessageId: {MessageId}",
                integrationEvent.TicketId,
                ticketCode.Value,
                byTicketId.TicketCode.Value,
                integrationEvent.MessageId);

            return true;
        }

        if (byTicketCode is not null && byTicketCode.Id != ticketId)
        {
            logger.LogError(
                "Ticket projection contract violation. TicketIssued has TicketCode {TicketCode} with TicketId {IncomingTicketId}, but existing projection for same TicketCode has TicketId {ExistingTicketId}. MessageId: {MessageId}",
                ticketCode.Value,
                integrationEvent.TicketId,
                byTicketCode.Id.Value,
                integrationEvent.MessageId);

            return true;
        }

        var existing = byTicketId ?? byTicketCode!;

        if (!StableFieldsMatch(existing, integrationEvent, ticketCode))
        {
            logger.LogError(
                "Ticket projection contract violation. TicketIssued stable fields do not match existing projection. MessageId: {MessageId}, TicketId: {TicketId}, TicketCode: {TicketCode}",
                integrationEvent.MessageId,
                integrationEvent.TicketId,
                ticketCode.Value);

            return true;
        }

        // Idempotent replay.
        // If the projection is already Canceled, do not reactivate it.
        return true;
    }

    private static bool StableFieldsMatch(
        TicketProjection projection,
        TicketIssuedIntegrationEvent integrationEvent,
        TicketCode ticketCode)
    {
        return projection.Id.Value == integrationEvent.TicketId
            && projection.TicketCode == ticketCode
            && projection.PublishedEventReferenceId.Value == integrationEvent.PublishedEventReferenceId
            && projection.OrderId.Value == integrationEvent.OrderId
            && projection.OrderItemId.Value == integrationEvent.OrderItemId
            && projection.InventoryId.Value == integrationEvent.InventoryId
            && projection.InventorySeatId?.Value == integrationEvent.InventorySeatId
            && projection.GeneralAdmissionPoolId?.Value == integrationEvent.GeneralAdmissionPoolId;
    }

    private static TicketProjection FromIntegrationEvent(
        TicketIssuedIntegrationEvent integrationEvent,
        TicketCode ticketCode)
    {
        return TicketProjection.Create(
            id: new TicketId(integrationEvent.TicketId),
            ticketCode: ticketCode,
            publishedEventReferenceId: new PublishedEventReferenceId(integrationEvent.PublishedEventReferenceId),
            orderId: new OrderId(integrationEvent.OrderId),
            orderItemId: new OrderItemId(integrationEvent.OrderItemId),
            inventoryId: new InventoryId(integrationEvent.InventoryId),
            inventorySeatId: integrationEvent.InventorySeatId.HasValue
                ? new InventorySeatId(integrationEvent.InventorySeatId.Value)
                : null,
            generalAdmissionPoolId: integrationEvent.GeneralAdmissionPoolId.HasValue
                ? new GeneralAdmissionPoolId(integrationEvent.GeneralAdmissionPoolId.Value)
                : null,
            issuedAt: integrationEvent.OccurredOn);
    }

    internal static bool IsDuplicateTicketProjection(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;

        if (exception.InnerException is SqlException sqlException)
        {
            return sqlException.Number is 2601 or 2627
                && (
                    message.Contains("PK_ticket_projections", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("IX_ticket_projections_ticket_code", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("ticket_projections", StringComparison.OrdinalIgnoreCase)
                );
        }

        return message.Contains("PK_ticket_projections", StringComparison.OrdinalIgnoreCase)
            || message.Contains("IX_ticket_projections_ticket_code", StringComparison.OrdinalIgnoreCase);
    }
}