using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Events.Features.GetVenue;

public static class GetVenueEndpoint
{
    public sealed record GetVenueResponse(
        Guid VenueId,
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    public static IEndpointRouteBuilder MapGetVenue(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events/venues/{id:guid}", Handle)
            .WithName("GetVenue")
            .WithTags("Events")
            .RequireAuthorization()
            .Produces<GetVenueResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid id,
        GetVenueHandler handler,
        CancellationToken ct)
    {
        GetVenueQuery query = new(id);

        Result<GetVenueResult> result = await handler.Handle(query, ct);

        return result.Match(ToOk, ToProblem);
    }

    private static IResult ToOk(GetVenueResult result)
    {
        GetVenueResponse response = new(
            VenueId: result.VenueId,
            Name: result.Name,
            Address: result.StreetAddress,
            City: result.City,
            Country: result.Country,
            Capacity: result.Capacity);

        return Results.Ok(response);
    }
}
