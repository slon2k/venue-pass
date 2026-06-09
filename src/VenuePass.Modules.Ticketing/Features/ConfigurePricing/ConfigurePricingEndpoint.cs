using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.ConfigurePricing;

public static class ConfigurePricingEndpoint
{
    public sealed record ConfigurePricingRequest(
        IReadOnlyList<PriceZoneRequestItem> PriceZones);

    public sealed record PriceZoneRequestItem(
        string Name,
        decimal Price,
        IReadOnlyList<Guid>? SeatIds,
        IReadOnlyList<Guid>? PoolIds);

    public static IEndpointRouteBuilder MapConfigurePricing(this IEndpointRouteBuilder app)
    {
        app.MapPut("/offers/{offerId:guid}/price-zones", Handle)
            .WithName("ConfigurePricing")
            .WithTags("Ticketing")
            .RequireAuthorization("EventManager")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid offerId,
        [FromBody] ConfigurePricingRequest request,
        ConfigurePricingHandler handler,
        CancellationToken ct)
    {
        ConfigurePricingCommand command = new(
            OfferId: offerId,
            PriceZones: [.. request.PriceZones.Select(z => new PriceZoneCommandItem(
                Name: z.Name,
                Price: z.Price,
                SeatIds: z.SeatIds ?? [],
                PoolIds: z.PoolIds ?? []
            ))]);

        Result<ConfigurePricingResult> result = await handler.Handle(command, ct);

        return result.Match(_ => Results.NoContent(), ToProblem);
    }
}
