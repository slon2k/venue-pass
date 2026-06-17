using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Ticketing.Domain.Tickets;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.GetTicket;

public sealed record GetTicketQuery(string TicketCode);

public sealed record GetTicketResult(
    Guid TicketId,
    string Code,
    string Status,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    DateTimeOffset CreatedAt);

public sealed class GetTicketHandler(TicketingDbContext db)
{
    public async Task<Result<GetTicketResult>> Handle(GetTicketQuery query, CancellationToken ct)
    {
        var ticket = await db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code.Value == query.TicketCode, ct);

        if (ticket is null)
        {
            return GetTicketErrors.TicketNotFound(query.TicketCode);
        }

        return new GetTicketResult(
            TicketId: ticket.Id.Value,
            Code: ticket.Code.Value,
            Status: ticket.Status.ToString(),
            InventorySeatId: ticket.InventorySeatId?.Value,
            GeneralAdmissionPoolId: ticket.GeneralAdmissionPoolId?.Value,
            CreatedAt: ticket.CreatedAt);
    }
}