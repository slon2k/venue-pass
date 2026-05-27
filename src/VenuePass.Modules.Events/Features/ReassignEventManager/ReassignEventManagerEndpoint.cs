using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Events.Features.ReassignEventManager;

public static class ReassignEventManagerEndpoint
{
    public sealed record ReassignEventManagerRequest(Guid NewManagerId);

    public static IEndpointRouteBuilder MapReassignEventManager(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{id:guid}/reassign-manager", Handle)
            .WithName("ReassignEventManager")
            .WithTags("Events")
            .RequireAuthorization("EventAdmin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid id,
        ReassignEventManagerRequest request,
        ReassignEventManagerHandler handler,
        CancellationToken ct)
    {
        ReassignEventManagerCommand command = new(
            EventId: id,
            NewManagerId: request.NewManagerId);

        var result = await handler.Handle(command, ct);

        return result.Match(
            onSuccess: Results.NoContent,
            onFailure: ToProblem);
    }
}
