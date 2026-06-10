using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.GetInventoryStatus;

public static class GetInventoryStatusEndpoint
{
    public static IEndpointRouteBuilder MapGetInventoryStatus(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events/{eventId:guid}/inventory", Handle)
            .WithName("GetInventoryStatus")
            .WithTags("Ticketing")
            .RequireAuthorization()
            .Produces<GetInventoryStatusResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid eventId,
        GetInventoryStatusHandler handler,
        CancellationToken ct)
    {
        GetInventoryStatusQuery query = new(eventId);

        Result<GetInventoryStatusResult> result = await handler.Handle(query, ct);

        return result.Match(r => Results.Ok(ToResponse(r)), ToProblem);
    }

    private static GetInventoryStatusResponse ToResponse(GetInventoryStatusResult result) =>
        new(
            EventId: result.EventId,
            InventoryId: result.InventoryId,
            TotalSeats: result.TotalSeats,
            AvailableSeats: result.AvailableSeats,
            Sections: [.. result.Sections.Select(s => new SectionStatusResponse(s.Name, s.TotalSeats, s.AvailableSeats))],
            Pools: [.. result.Pools.Select(p => new PoolStatusResponse(p.Name, p.TotalCapacity, p.AvailableCount))]);
}

public sealed record GetInventoryStatusResponse(
    Guid EventId,
    Guid InventoryId,
    int TotalSeats,
    int AvailableSeats,
    IReadOnlyList<SectionStatusResponse> Sections,
    IReadOnlyList<PoolStatusResponse> Pools);

public sealed record SectionStatusResponse(string Name, int TotalSeats, int AvailableSeats);

public sealed record PoolStatusResponse(string Name, int TotalCapacity, int AvailableCount);
