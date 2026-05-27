using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Events.Features.CreateManifestTemplate;

public static class CreateManifestTemplateEndpoint
{
    public sealed record CreateManifestTemplateRequest(
        [property: MaxLength(ManifestTemplateName.MaxLength)] string Name,
        [property: MaxLength(ManifestTemplateDescription.MaxLength)] string? Description,
        Guid VenueId,
        IReadOnlyList<CreateManifestTemplateSectionRequest>? Sections,
        IReadOnlyList<CreateManifestTemplateGeneralAdmissionAreaRequest>? GeneralAdmissionAreas);

    public sealed record CreateManifestTemplateSectionRequest(
        [property: MaxLength(SectionName.MaxLength)] string Name,
        IReadOnlyList<CreateManifestTemplateRowRequest>? Rows);

    public sealed record CreateManifestTemplateRowRequest(
        [property: MaxLength(RowLabel.MaxLength)] string Label,
        IReadOnlyList<CreateManifestTemplateSeatRequest>? Seats);

    public sealed record CreateManifestTemplateSeatRequest(
        [property: MaxLength(SeatLabel.MaxLength)] string Label);

    public sealed record CreateManifestTemplateGeneralAdmissionAreaRequest(
        [property: MaxLength(GeneralAdmissionAreaName.MaxLength)] string Name,
        [property: Range(1, int.MaxValue)] int Capacity);

    public sealed record CreateManifestTemplateResponse(Guid ManifestTemplateId);

    public static IEndpointRouteBuilder MapCreateManifestTemplate(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/manifest-templates", Handle)
            .WithName("CreateManifestTemplate")
            .WithTags("Events")
            .RequireAuthorization("EventAdmin")
            .Produces<CreateManifestTemplateResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        CreateManifestTemplateRequest request,
        CreateManifestTemplateHandler handler,
        CancellationToken ct)
    {
        CreateManifestTemplateCommand command = new(
            Name: request.Name,
            Description: request.Description,
            VenueId: request.VenueId,
            Sections: [.. (request.Sections ?? []).Select(ToSectionCommand)],
            GeneralAdmissionAreas: [.. (request.GeneralAdmissionAreas ?? []).Select(ToAreaCommand)]);

        Result<CreateManifestTemplateResult> result = await handler.Handle(command, ct);

        return result.Match(ToCreated, ToProblem);
    }

    private static CreateManifestTemplateSectionCommand ToSectionCommand(
        CreateManifestTemplateSectionRequest section) =>
        new(
            Name: section.Name,
            Rows: [.. (section.Rows ?? []).Select(ToRowCommand)]);

    private static CreateManifestTemplateRowCommand ToRowCommand(CreateManifestTemplateRowRequest row) =>
        new(
            Label: row.Label,
            Seats: [.. (row.Seats ?? []).Select(seat => new CreateManifestTemplateSeatCommand(seat.Label))]);

    private static CreateManifestTemplateGeneralAdmissionAreaCommand ToAreaCommand(
        CreateManifestTemplateGeneralAdmissionAreaRequest area) =>
        new(
            Name: area.Name,
            Capacity: area.Capacity);

    private static IResult ToCreated(CreateManifestTemplateResult result)
    {
        CreateManifestTemplateResponse response = new(result.ManifestTemplateId);

        return Results.Created($"/events/manifest-templates/{response.ManifestTemplateId}", response);
    }
}
