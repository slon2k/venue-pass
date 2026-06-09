using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.CreateOffer;

public static class CreateOfferEndpoint
{
    public sealed record CreateOfferRequest(
        string Name,
        string Currency,
        DateTimeOffset? SaleStart,
        DateTimeOffset? SaleEnd);

    public sealed record CreateOfferResponse(Guid OfferId);

    public static IEndpointRouteBuilder MapCreateOffer(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{eventId:guid}/offers", Handle)
            .WithName("CreateOffer")
            .WithTags("Ticketing")
            .RequireAuthorization("EventManager")
            .Produces<CreateOfferResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid eventId,
        [FromBody] CreateOfferRequest request,
        CreateOfferHandler handler,
        CancellationToken ct)
    {
        CreateOfferCommand command = new(
            EventId: eventId,
            Name: request.Name,
            Currency: request.Currency,
            SaleStart: request.SaleStart,
            SaleEnd: request.SaleEnd);

        Result<CreateOfferResult> result = await handler.Handle(command, ct);

        return result.Match(ToCreated, ToProblem);
    }

    private static IResult ToCreated(CreateOfferResult result) =>
        Results.Created($"/offers/{result.OfferId}", new CreateOfferResponse(result.OfferId));
}
