using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.GetTicket;

public sealed record GetTicketResponse(
    Guid TicketId,
    string Code,
    string Status,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    DateTimeOffset CreatedAt);

public static class GetTicketEndpoint
{
    public static IEndpointRouteBuilder MapGetTicket(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tickets/{ticketCode}", Handle)
            .WithName("GetTicket")
            .WithTags("Tickets")
            .RequireAuthorization()
            .Produces<GetTicketResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(string ticketCode, GetTicketHandler handler, CancellationToken ct)
    {
        var result = await handler.Handle(new GetTicketQuery(ticketCode), ct);

        return result.Match(ToOk, ToProblem);
    }

    private static IResult ToOk(GetTicketResult result) =>
        Results.Ok(new GetTicketResponse(
            TicketId: result.TicketId,
            Code: result.Code,
            InventorySeatId: result.InventorySeatId,
            GeneralAdmissionPoolId: result.GeneralAdmissionPoolId,
            Status: result.Status,
            CreatedAt: result.CreatedAt));
}