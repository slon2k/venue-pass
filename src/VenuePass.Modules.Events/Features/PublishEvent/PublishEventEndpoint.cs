using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Events.Features.PublishEvent;

public static class PublishEventEndpoint
{
    public static IEndpointRouteBuilder MapPublishEvent(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{id:guid}/publish", Handle)
            .WithName("PublishEvent")
            .WithTags("Events")
            .RequireAuthorization("EventManager")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal user,
        PublishEventHandler handler,
        CancellationToken ct)
    {
        var callerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var command = new PublishEventCommand(EventId: id, CallerId: callerId);
        var result = await handler.Handle(command, ct);

        return result.Match(
            onSuccess: Results.NoContent,
            onFailure: ToProblem);
    }
}
