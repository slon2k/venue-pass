using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

using DomainEventName = VenuePass.Modules.Events.Domain.Events.EventName;
using DomainEventDescription = VenuePass.Modules.Events.Domain.Events.EventDescription;

namespace VenuePass.Modules.Events.Features.CreateEvent;

public static class CreateEventEndpoint
{
    public sealed record CreateEventRequest(
        Guid VenueId,
        Guid ManifestTemplateId,
        [property: MaxLength(DomainEventName.MaxLength)] string Name,
        DateTimeOffset EventDate,
        [property: MaxLength(DomainEventDescription.MaxLength)] string? Description);

    public sealed record CreateEventResponse(
        Guid EventId,
        Guid ManifestId);

    public static IEndpointRouteBuilder MapCreateEvent(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events", Handle)
            .WithName("CreateEvent")
            .WithTags("Events")
            .RequireAuthorization("EventManager")
            .Produces<CreateEventResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        CreateEventRequest request,
        CreateEventHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var sub = httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (sub is null || !Guid.TryParse(sub, out Guid subGuid))
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }

        CreateEventCommand command = new(
            VenueId: request.VenueId,
            ManifestTemplateId: request.ManifestTemplateId,
            Name: request.Name,
            EventDate: request.EventDate,
            Description: request.Description,
            AssignedManagerId: new UserId(subGuid));

        Result<CreateEventResult> result = await handler.Handle(command, ct);

        return result.Match(ToCreated, ToProblem);
    }

    private static IResult ToCreated(CreateEventResult result)
    {
        CreateEventResponse response = new(
            EventId: result.EventId,
            ManifestId: result.ManifestId);

        return Results.Created($"/events/{response.EventId}", response);
    }
}
