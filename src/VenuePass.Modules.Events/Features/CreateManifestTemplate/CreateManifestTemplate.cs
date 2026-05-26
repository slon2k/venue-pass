using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Infrastructure;

namespace VenuePass.Modules.Events.Features.CreateManifestTemplate;

public sealed class CreateManifestTemplateHandler(
    EventsDbContext db,
    IValidator<CreateManifestTemplateCommand> validator,
    ILogger<CreateManifestTemplateHandler> logger)
{
    public async Task<Result<CreateManifestTemplateResult>> Handle(
        CreateManifestTemplateCommand command,
        CancellationToken ct)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            return CreateManifestTemplateErrors.InvalidData(
                [.. validationResult.Errors.Select(e =>
                    new ValidationErrorDetail(e.PropertyName, e.ErrorMessage))]);
        }

        var venueId = new VenueId(command.VenueId);

        if (!await db.Venues.AnyAsync(v => v.Id == venueId, ct))
        {
            return CreateManifestTemplateErrors.VenueNotFound(command.VenueId);
        }

        ManifestTemplate template;

        try
        {
            template = ToEntity(command, venueId);
        }
        catch (DomainRuleViolationException ex)
        {
            logger.LogInformation(ex, "Domain validation rejected manifest template creation.");
            return CreateManifestTemplateErrors.InvalidData(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogInformation(ex, "Value object validation rejected manifest template creation.");
            return CreateManifestTemplateErrors.InvalidData(ex.Message);
        }
        
        db.ManifestTemplates.Add(template);
        await db.SaveChangesAsync(ct);

        return new CreateManifestTemplateResult(template.Id);
    }

    private static ManifestTemplate ToEntity(CreateManifestTemplateCommand command, VenueId venueId)
    {
        ManifestTemplateDescription? description = command.Description is null
            ? null
            : new ManifestTemplateDescription(command.Description);

        IReadOnlyList<SectionDraft> sectionDrafts = [.. command.Sections.Select(ToSectionDraft)];
        IReadOnlyList<GeneralAdmissionAreaDraft> areaDrafts = [.. command.GeneralAdmissionAreas.Select(ToGeneralAdmissionAreaDraft)];

        return ManifestTemplate.Create(
            name: new ManifestTemplateName(command.Name),
            description: description,
            venueId: venueId,
            sectionDrafts: sectionDrafts,
            generalAdmissionAreaDrafts: areaDrafts);
    }

    private static SectionDraft ToSectionDraft(CreateManifestTemplateSectionCommand section) =>
        new(
            Name: section.Name,
            Rows: [.. section.Rows.Select(ToRowDraft)]);

    private static RowDraft ToRowDraft(CreateManifestTemplateRowCommand row) =>
        new(
            Label: row.Label,
            Seats: [.. row.Seats.Select(seat => new SeatDraft(seat.Label))]);

    private static GeneralAdmissionAreaDraft ToGeneralAdmissionAreaDraft(
        CreateManifestTemplateGeneralAdmissionAreaCommand area) =>
        new(
            Name: area.Name,
            Capacity: area.Capacity);
}

public sealed record CreateManifestTemplateCommand(
    string Name,
    string? Description,
    Guid VenueId,
    IReadOnlyList<CreateManifestTemplateSectionCommand> Sections,
    IReadOnlyList<CreateManifestTemplateGeneralAdmissionAreaCommand> GeneralAdmissionAreas);

public sealed record CreateManifestTemplateSectionCommand(
    string Name,
    IReadOnlyList<CreateManifestTemplateRowCommand> Rows);

public sealed record CreateManifestTemplateRowCommand(
    string Label,
    IReadOnlyList<CreateManifestTemplateSeatCommand> Seats);

public sealed record CreateManifestTemplateSeatCommand(string Label);

public sealed record CreateManifestTemplateGeneralAdmissionAreaCommand(
    string Name,
    int Capacity);

public sealed record CreateManifestTemplateResult(Guid ManifestTemplateId);
