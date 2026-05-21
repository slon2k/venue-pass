using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Features.CreateVenue;

public static class CreateVenueEndpoint
{
    public sealed record CreateVenueRequest(
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    public sealed record CreateVenueResponse(
        Guid VenueId,
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    public static IEndpointRouteBuilder MapCreateVenue(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/venues", Handle)
            .WithName("CreateVenue")
            .WithTags("Events")
            .Produces<CreateVenueResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        CreateVenueRequest request,
        CreateVenueHandler handler,
        CancellationToken ct)
    {
        CreateVenueCommand command = new(
            request.Name,
            request.Address,
            request.City,
            request.Country,
            request.Capacity);

        Result<CreateVenueResult> result = await handler.Handle(command, ct);

        return result.Match(
            ToCreatedResult,
            error => error.ToProblemDetails());
    }

    private static IResult ToCreatedResult(this CreateVenueResult result)
    {
        CreateVenueResponse response = new (
            result.VenueId,
            result.Name,
            result.StreetAddress,
            result.City,
            result.Country,
            result.Capacity);

        return Results.Created($"/events/venues/{response.VenueId}", response);
    }
}