using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.GetOffers;

public static class GetOffersEndpoint
{
    public static IEndpointRouteBuilder MapGetOffers(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events/{eventId:guid}/offers", Handle)
            .WithName("GetOffers")
            .WithTags("Ticketing")
            .RequireAuthorization()
            .Produces<GetOffersResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid eventId,
        GetOffersHandler handler,
        CancellationToken ct)
    {
        GetOffersQuery query = new(eventId);

        Result<GetOffersResult> result = await handler.Handle(query, ct);

        return result.Match(Results.Ok, ToProblem);
    }
}
