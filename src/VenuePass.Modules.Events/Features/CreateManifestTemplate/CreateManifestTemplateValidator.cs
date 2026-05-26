using FluentValidation;

using VenuePass.Modules.Events.Domain.ManifestTemplates;

namespace VenuePass.Modules.Events.Features.CreateManifestTemplate;

public sealed class CreateManifestTemplateValidator : AbstractValidator<CreateManifestTemplateCommand>
{
    public CreateManifestTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(ManifestTemplateName.MaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(ManifestTemplateDescription.MaxLength)
            .When(x => x.Description is not null);

        RuleFor(x => x.Description)
            .Must(description => !string.IsNullOrWhiteSpace(description))
            .When(x => x.Description is not null)
            .WithMessage("Description cannot be empty.");

        RuleFor(x => x.VenueId)
            .NotEmpty();

        RuleFor(x => x.Sections)
            .NotNull();

        RuleFor(x => x.GeneralAdmissionAreas)
            .NotNull();

        RuleFor(x => x)
            .Must(HaveAtLeastOneLayoutElement)
            .WithMessage("Manifest template must contain at least one layout element.");

        RuleForEach(x => x.Sections)
            .SetValidator(new CreateManifestTemplateSectionValidator());

        RuleForEach(x => x.GeneralAdmissionAreas)
            .SetValidator(new CreateManifestTemplateGeneralAdmissionAreaValidator());
    }

    private static bool HaveAtLeastOneLayoutElement(CreateManifestTemplateCommand command)
    {
        int sectionsCount = command.Sections?.Count ?? 0;
        int areaCount = command.GeneralAdmissionAreas?.Count ?? 0;

        return sectionsCount + areaCount > 0;
    }
}

internal sealed class CreateManifestTemplateSectionValidator : AbstractValidator<CreateManifestTemplateSectionCommand>
{
    public CreateManifestTemplateSectionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(SectionName.MaxLength);

        RuleFor(x => x.Rows)
            .NotNull()
            .NotEmpty();

        RuleForEach(x => x.Rows)
            .SetValidator(new CreateManifestTemplateRowValidator());
    }
}

internal sealed class CreateManifestTemplateRowValidator : AbstractValidator<CreateManifestTemplateRowCommand>
{
    public CreateManifestTemplateRowValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(RowLabel.MaxLength);

        RuleFor(x => x.Seats)
            .NotNull()
            .NotEmpty();

        RuleForEach(x => x.Seats)
            .SetValidator(new CreateManifestTemplateSeatValidator());
    }
}

internal sealed class CreateManifestTemplateSeatValidator : AbstractValidator<CreateManifestTemplateSeatCommand>
{
    public CreateManifestTemplateSeatValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(SeatLabel.MaxLength);
    }
}

internal sealed class CreateManifestTemplateGeneralAdmissionAreaValidator : AbstractValidator<CreateManifestTemplateGeneralAdmissionAreaCommand>
{
    public CreateManifestTemplateGeneralAdmissionAreaValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(GeneralAdmissionAreaName.MaxLength);

        RuleFor(x => x.Capacity)
            .GreaterThan(0);
    }
}
