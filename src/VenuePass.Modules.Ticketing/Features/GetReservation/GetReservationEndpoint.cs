using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.GetReservation;

public static class GetReservationEndpoint
{
    public static IEndpointRouteBuilder MapGetReservation(this IEndpointRouteBuilder app)
    {
        app.MapGet("/reservations/{reservationId:guid}", Handle)
            .WithName("GetReservation")
            .WithTags("Reservations")
            .RequireAuthorization()
            .Produces<GetReservationResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid reservationId,
        GetReservationHandler handler,
        CancellationToken ct)
    {
        var query = new GetReservationQuery(reservationId);
        var result = await handler.Handle(query, ct);

        return result.Match(Results.Ok, ToProblem);
    }
}