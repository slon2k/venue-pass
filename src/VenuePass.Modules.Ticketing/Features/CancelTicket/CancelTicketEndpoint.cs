using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using VenuePass.Modules.Ticketing.Domain.Tickets;

using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.CancelTicket;

public static class CancelTicketEndpoint
{
    public static IEndpointRouteBuilder MapCancelTicket(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/tickets/{ticketId:guid}", Handle)
            .WithName("CancelTicket")
            .WithTags("Tickets")
            .RequireAuthorization("EventManager")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid ticketId,
        CancelTicketHandler handler,
        CancellationToken ct)
    {
        var command = new CancelTicketCommand(new TicketId(ticketId));

        var result = await handler.Handle(command, ct);

        return result.Match(Results.NoContent, ToProblem);
    }
}

