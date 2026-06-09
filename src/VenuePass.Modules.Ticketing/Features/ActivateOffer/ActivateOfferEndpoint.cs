using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.ActivateOffer;

public static class ActivateOfferEndpoint
{
    public static IEndpointRouteBuilder MapActivateOffer(this IEndpointRouteBuilder app)
    {
        app.MapPost("/offers/{offerId:guid}/activate", Handle)
            .WithName("ActivateOffer")
            .WithTags("Ticketing")
            .RequireAuthorization("EventManager")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid offerId,
        ActivateOfferHandler handler,
        CancellationToken ct)
    {
        ActivateOfferCommand command = new(OfferId: offerId);

        Result<ActivateOfferResult> result = await handler.Handle(command, ct);

        return result.Match(_ => Results.NoContent(), ToProblem);
    }
}
