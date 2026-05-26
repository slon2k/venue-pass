using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Events.Features.GetManifestTemplate;

public static class GetManifestTemplateEndpoint
{
    public sealed record GetManifestTemplateResponse(
        Guid ManifestTemplateId,
        string Name,
        string? Description,
        Guid VenueId,
        IReadOnlyList<GetManifestTemplateSectionResponse> Sections,
        IReadOnlyList<GetManifestTemplateGeneralAdmissionAreaResponse> GeneralAdmissionAreas);

    public sealed record GetManifestTemplateSectionResponse(
        string Name,
        IReadOnlyList<GetManifestTemplateRowResponse> Rows);

    public sealed record GetManifestTemplateRowResponse(
        string Label,
        IReadOnlyList<GetManifestTemplateSeatResponse> Seats);

    public sealed record GetManifestTemplateSeatResponse(string Label);

    public sealed record GetManifestTemplateGeneralAdmissionAreaResponse(
        string Name,
        int Capacity);

    public static IEndpointRouteBuilder MapGetManifestTemplate(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events/manifest-templates/{id:guid}", Handle)
            .WithName("GetManifestTemplate")
            .WithTags("Events")
            .Produces<GetManifestTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid id,
        GetManifestTemplateHandler handler,
        CancellationToken ct)
    {
        GetManifestTemplateQuery query = new(id);

        Result<GetManifestTemplateResult> result = await handler.Handle(query, ct);

        return result.Match(ToOk, ToProblem);
    }

    private static IResult ToOk(GetManifestTemplateResult result)
    {
        GetManifestTemplateResponse response = new(
            ManifestTemplateId: result.ManifestTemplateId,
            Name: result.Name,
            Description: result.Description,
            VenueId: result.VenueId,
            Sections:
            [
                .. result.Sections.Select(section => new GetManifestTemplateSectionResponse(
                    Name: section.Name,
                    Rows:
                    [
                        .. section.Rows.Select(row => new GetManifestTemplateRowResponse(
                            Label: row.Label,
                            Seats:
                            [
                                .. row.Seats.Select(seat => new GetManifestTemplateSeatResponse(seat.Label))
                            ]))
                    ]))
            ],
            GeneralAdmissionAreas:
            [
                .. result.GeneralAdmissionAreas.Select(area => new GetManifestTemplateGeneralAdmissionAreaResponse(
                    Name: area.Name,
                    Capacity: area.Capacity))
            ]);

        return Results.Ok(response);
    }
}
