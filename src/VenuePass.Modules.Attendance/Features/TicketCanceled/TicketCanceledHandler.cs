using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Messaging;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Infrastructure;
using VenuePass.Modules.Ticketing.Contracts;
using VenuePass.Modules.Attendance.Domain.TicketProjections;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using Microsoft.Data.SqlClient;

namespace VenuePass.Modules.Attendance.Features.TicketCanceled;

internal sealed class TicketCanceledHandler(
    AttendanceDbContext db,
    ILogger<TicketCanceledHandler> logger)
    : IIntegrationEventHandler<TicketCanceledIntegrationEvent>
{
    public async Task Handle(
        TicketCanceledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!TicketCode.TryCreate(integrationEvent.TicketCode, out var ticketCode))
        {
            logger.LogError(
                "Received TicketCanceled event with malformed ticket code. MessageId: {MessageId}, TicketId: {TicketId}, TicketCode: {TicketCode}",
                integrationEvent.MessageId,
                integrationEvent.TicketId,
                integrationEvent.TicketCode);

            throw new InvalidOperationException(
                $"TicketCanceled event contains invalid ticket code '{integrationEvent.TicketCode}'.");
        }

        var ticketId = new TicketId(integrationEvent.TicketId);

        if (ticketId.IsEmpty)
            throw new InvalidOperationException("Ticket ID cannot be empty.");

        var ticketProjection = await db.TicketProjections.FindAsync([ticketId], cancellationToken);

        if (ticketProjection is null)
        {
            var ticketByCodeProjection = await db.TicketProjections
                .SingleOrDefaultAsync(projection => projection.TicketCode == ticketCode, cancellationToken);

            if (ticketByCodeProjection is not null)
            {
                logger.LogError(
                    "Received TicketCanceled event for ticket code that already exists with a different ticket ID. MessageId: {MessageId}, IncomingTicketId: {IncomingTicketId}, ExistingTicketId: {ExistingTicketId}, TicketCode: {TicketCode}",
                    integrationEvent.MessageId,
                    integrationEvent.TicketId,
                    ticketByCodeProjection.Id.Value,
                    ticketCode.Value);

                throw new InvalidOperationException("Ticket projection contract violation: TicketCode/id mismatch.");
            }

            // If the ticket projection does not exist, we create a new one with the canceled status.
            ticketProjection = FromIntegrationEvent(integrationEvent, ticketId, ticketCode);
            db.TicketProjections.Add(ticketProjection);
        } 
        else
        {
            if (!StableFieldsMatch(ticketProjection, integrationEvent, ticketCode))
            {
                logger.LogError(
                    "Received TicketCanceled event for ticket projection with mismatched stable fields. MessageId: {MessageId}, TicketId: {TicketId}, TicketCode: {TicketCode}",
                    integrationEvent.MessageId,
                    integrationEvent.TicketId,
                    integrationEvent.TicketCode);

                throw new InvalidOperationException("Ticket projection contract violation: Stable fields mismatch.");
            }

            if (!ticketProjection.Cancel(integrationEvent.OccurredOn))
            {
                // The ticket projection is already canceled.
                return;
            }   
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();

            var existingProjection = await db.TicketProjections
                .SingleOrDefaultAsync(projection =>
                    projection.Id == ticketId,
                    cancellationToken);

            if (existingProjection is null)
            {
                logger.LogError(
                    "Concurrency conflict occurred while processing TicketCanceled event, but projection no longer exists. MessageId: {MessageId}, TicketId: {TicketId}, TicketCode: {TicketCode}",
                    integrationEvent.MessageId,
                    integrationEvent.TicketId,
                    integrationEvent.TicketCode);

                throw new InvalidOperationException(
                    $"Concurrency conflict occurred while processing TicketCanceled event for ticket '{integrationEvent.TicketCode}', but projection no longer exists.");
            }
            
            if (!StableFieldsMatch(existingProjection, integrationEvent, ticketCode))
            {
                logger.LogError(
                    "Concurrency conflict occurred while processing TicketCanceled event. The ticket projection was modified by another process with mismatched stable fields. MessageId: {MessageId}, TicketId: {TicketId}, TicketCode: {TicketCode}",
                    integrationEvent.MessageId,
                    integrationEvent.TicketId,
                    integrationEvent.TicketCode);

                throw new InvalidOperationException(
                    $"Concurrency conflict occurred while processing TicketCanceled event for ticket '{integrationEvent.TicketCode}' with mismatched stable fields.");
            }

            if (!(existingProjection.Status == TicketProjectionStatus.Canceled))
            {
                logger.LogWarning(
                    "Concurrency conflict occurred while processing TicketCanceled event. The ticket projection was modified by another process. MessageId: {MessageId}, TicketId: {TicketId}, TicketCode: {TicketCode}",
                    integrationEvent.MessageId,
                    integrationEvent.TicketId,
                    integrationEvent.TicketCode);

                throw new InvalidOperationException(
                    $"Concurrency conflict occurred while processing TicketCanceled event for ticket '{integrationEvent.TicketCode}'.");
            }
        }
        catch (DbUpdateException ex) when (IsDuplicateTicketProjection(ex))
        {
            db.ChangeTracker.Clear();

            var existingById = await db.TicketProjections
                .SingleOrDefaultAsync(p => p.Id == ticketId, cancellationToken);

            if (existingById is not null)
            {
                if (!StableFieldsMatch(existingById, integrationEvent, ticketCode))
                {
                    logger.LogError(
                        "Duplicate ticket projection detected with mismatched stable fields. MessageId: {MessageId}, TicketId: {TicketId}, TicketCode: {TicketCode}",
                        integrationEvent.MessageId,
                        integrationEvent.TicketId,
                        integrationEvent.TicketCode);

                    throw new InvalidOperationException(
                        "Ticket projection contract violation: duplicate TicketId with mismatched stable fields.");
                }

                if (existingById.Status == TicketProjectionStatus.Canceled)
                {
                    // Another handler already converged the projection.
                    return;
                }

                logger.LogWarning(
                    "TicketCanceled projection insert raced with another projection insert. Existing projection is Issued; message will be retried by outbox dispatcher. MessageId: {MessageId}, TicketId: {TicketId}, TicketCode: {TicketCode}",
                    integrationEvent.MessageId,
                    integrationEvent.TicketId,
                    integrationEvent.TicketCode);

                throw new InvalidOperationException(
                    "TicketCanceled projection insert raced with another insert. Retry required.");
            }

            var existingByCode = await db.TicketProjections
                .SingleOrDefaultAsync(p => p.TicketCode == ticketCode, cancellationToken);

            if (existingByCode is not null)
            {
                logger.LogError(
                    "Ticket projection contract violation. TicketCode already exists with a different TicketId. MessageId: {MessageId}, IncomingTicketId: {IncomingTicketId}, ExistingTicketId: {ExistingTicketId}, TicketCode: {TicketCode}",
                    integrationEvent.MessageId,
                    integrationEvent.TicketId,
                    existingByCode.Id.Value,
                    ticketCode.Value);

                throw new InvalidOperationException(
                    "Ticket projection contract violation: TicketCode/id mismatch.");
            }

            logger.LogError(
                "Duplicate ticket projection detected, but no projection was found by TicketId or TicketCode. MessageId: {MessageId}, TicketId: {TicketId}, TicketCode: {TicketCode}",
                integrationEvent.MessageId,
                integrationEvent.TicketId,
                integrationEvent.TicketCode);

            throw;
        }
    }

    private static TicketProjection FromIntegrationEvent(
        TicketCanceledIntegrationEvent integrationEvent,
        TicketId ticketId,
        TicketCode ticketCode)
    {
        return TicketProjection.CreateCanceled(
            id: ticketId,
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
            canceledAt: integrationEvent.OccurredOn);
    }

    private static bool StableFieldsMatch(
        TicketProjection projection,
        TicketCanceledIntegrationEvent integrationEvent,
        TicketCode ticketCode) => 
            projection.Id.Value == integrationEvent.TicketId
            && projection.TicketCode == ticketCode
            && projection.PublishedEventReferenceId.Value == integrationEvent.PublishedEventReferenceId
            && projection.OrderId.Value == integrationEvent.OrderId
            && projection.OrderItemId.Value == integrationEvent.OrderItemId
            && projection.InventoryId.Value == integrationEvent.InventoryId
            && projection.InventorySeatId?.Value == integrationEvent.InventorySeatId
            && projection.GeneralAdmissionPoolId?.Value == integrationEvent.GeneralAdmissionPoolId;

    private static bool IsDuplicateTicketProjection(DbUpdateException exception)
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