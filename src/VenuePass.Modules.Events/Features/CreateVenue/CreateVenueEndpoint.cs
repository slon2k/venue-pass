using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Events.Domain.Venues;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Events.Features.CreateVenue;

public static class CreateVenueEndpoint
{
    public sealed record CreateVenueRequest(
    [property: MaxLength(VenueName.MaxLength)] string Name,
    [property: MaxLength(StreetAddress.MaxLength)] string Address,
    [property: MaxLength(City.MaxLength)] string City,
    [property: MaxLength(Country.MaxLength)] string Country,
    [property: Range(1, int.MaxValue)] int Capacity);

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
            .RequireAuthorization("EventAdmin")
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
            Name: request.Name,
            StreetAddress: request.Address,
            City: request.City,
            Country: request.Country,
            Capacity: request.Capacity);

        Result<CreateVenueResult> result = await handler.Handle(command, ct);

        return result.Match(ToCreated, ToProblem);
    }

    private static IResult ToCreated(CreateVenueResult result)
    {
        CreateVenueResponse response = new (
            VenueId: result.VenueId,
            Name: result.Name,
            Address: result.StreetAddress,
            City: result.City,
            Country: result.Country,
            Capacity: result.Capacity);

        return Results.Created($"/events/venues/{response.VenueId}", response);
    }
}