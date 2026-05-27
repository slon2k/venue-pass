using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Events.Features.GetEvent;

public static class GetEventEndpoint
{
    public static IEndpointRouteBuilder MapGetEvent(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events/{id:guid}", Handle)
            .WithName("GetEvent")
            .WithTags("Events")
            .RequireAuthorization()
            .Produces<GetEventResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid id,
        GetEventHandler handler,
        CancellationToken ct)
    {
        GetEventQuery query = new(id);

        Result<GetEventResult> result = await handler.Handle(query, ct);

        return result.Match(Results.Ok, ToProblem);
    }
}
