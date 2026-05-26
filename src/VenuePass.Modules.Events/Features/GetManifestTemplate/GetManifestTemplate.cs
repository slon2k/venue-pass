using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Infrastructure;

namespace VenuePass.Modules.Events.Features.GetManifestTemplate;

public sealed class GetManifestTemplateHandler(EventsDbContext db)
{
    public async Task<Result<GetManifestTemplateResult>> Handle(
        GetManifestTemplateQuery query,
        CancellationToken ct)
    {
        ManifestTemplate? template = await db.ManifestTemplates
            .AsNoTracking()
            .Include(x => x.Sections)
            .ThenInclude(x => x.Rows)
            .ThenInclude(x => x.Seats)
            .Include(x => x.GeneralAdmissionAreas)
            .FirstOrDefaultAsync(x => x.Id == new ManifestTemplateId(query.ManifestTemplateId), ct);

        if (template is null)
        {
            return GetManifestTemplateErrors.ManifestTemplateNotFound(query.ManifestTemplateId);
        }

        return ToResult(template);
    }

    private static GetManifestTemplateResult ToResult(ManifestTemplate template) => new(
        ManifestTemplateId: template.Id,
        Name: template.Name,
        Description: template.Description?.Value,
        VenueId: template.VenueId,
        Sections:
        [
            .. template.Sections.Select(section => new GetManifestTemplateSectionResult(
                Name: section.Name,
                Rows:
                [
                    .. section.Rows.Select(row => new GetManifestTemplateRowResult(
                        Label: row.Label,
                        Seats:
                        [
                            .. row.Seats.Select(seat => new GetManifestTemplateSeatResult(seat.Label))
                        ]))
                ]))
        ],
        GeneralAdmissionAreas:
        [
            .. template.GeneralAdmissionAreas.Select(area => new GetManifestTemplateGeneralAdmissionAreaResult(
                Name: area.Name,
                Capacity: area.Capacity))
        ]);
}

public sealed record GetManifestTemplateQuery(Guid ManifestTemplateId);

public sealed record GetManifestTemplateResult(
    Guid ManifestTemplateId,
    string Name,
    string? Description,
    Guid VenueId,
    IReadOnlyList<GetManifestTemplateSectionResult> Sections,
    IReadOnlyList<GetManifestTemplateGeneralAdmissionAreaResult> GeneralAdmissionAreas);

public sealed record GetManifestTemplateSectionResult(
    string Name,
    IReadOnlyList<GetManifestTemplateRowResult> Rows);

public sealed record GetManifestTemplateRowResult(
    string Label,
    IReadOnlyList<GetManifestTemplateSeatResult> Seats);

public sealed record GetManifestTemplateSeatResult(string Label);

public sealed record GetManifestTemplateGeneralAdmissionAreaResult(
    string Name,
    int Capacity);
