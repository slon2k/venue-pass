using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
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
        CancellationToken ct)
    {
        CreateEventCommand command = new(
            VenueId: request.VenueId,
            ManifestTemplateId: request.ManifestTemplateId,
            Name: request.Name,
            EventDate: request.EventDate,
            Description: request.Description);

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
