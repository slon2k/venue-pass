using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.GetOffer;

public static class GetOfferEndpoint
{
    public static IEndpointRouteBuilder MapGetOffer(this IEndpointRouteBuilder app)
    {
        app.MapGet("/offers/{offerId:guid}", Handle)
            .WithName("GetOffer")
            .WithTags("Ticketing")
            .RequireAuthorization()
            .Produces<GetOfferResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid offerId,
        GetOfferHandler handler,
        CancellationToken ct)
    {
        GetOfferQuery query = new(offerId);

        Result<GetOfferResult> result = await handler.Handle(query, ct);

        return result.Match(Results.Ok, ToProblem);
    }
}
