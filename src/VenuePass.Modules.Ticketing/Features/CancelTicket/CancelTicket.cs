using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Ticketing.Contracts;
using VenuePass.Modules.Ticketing.Domain.Tickets;
using VenuePass.Modules.Ticketing.Infrastructure;
using VenuePass.Modules.Ticketing.Infrastructure.Outbox;

namespace VenuePass.Modules.Ticketing.Features.CancelTicket;

public sealed record CancelTicketCommand(TicketId TicketId);

public sealed class CancelTicketHandler(
    TicketingDbContext db,
    TimeProvider timeProvider,
    ILogger<CancelTicketHandler> logger)
{
    public async Task<Result> Handle(
        CancelTicketCommand command,
        CancellationToken ct)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == command.TicketId, ct);

        if (ticket is null)
        {
            return CancelTicketErrors.TicketNotFound(command.TicketId);
        }

        var canceledAt = timeProvider.GetUtcNow();

        if (!ticket.Cancel(canceledAt))
        {
            return Result.Success();
        }

        var integrationEvent = new TicketCanceledIntegrationEvent(
            Guid.NewGuid(),
            ticket.Id.Value,
            ticket.Code.Value,
            ticket.PublishedEventReferenceId.Value,
            canceledAt);

        db.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation(
                "Concurrency conflict occurred while cancelling ticket with ID {TicketId}.",
                command.TicketId);

            return CancelTicketErrors.ConcurrencyConflict();
        }

        return Result.Success();
    }
}



