using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.CancelReservation;

public static class CancelReservationEndpoint
{
    public static IEndpointRouteBuilder MapCancelReservation(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/reservations/{reservationId:guid}", Handle)
            .WithName("CancelReservation")
            .WithTags("Reservations")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid reservationId,
        CancelReservationHandler handler,
        CancellationToken ct)
    {
        var command = new CancelReservationCommand(reservationId);
        var result = await handler.Handle(command, ct);

        return result.Match(Results.NoContent, ToProblem);
    }
}