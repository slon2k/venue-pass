using FluentValidation;

namespace VenuePass.Modules.Events.Features.GetManifestTemplate;

public sealed class GetManifestTemplateValidator : AbstractValidator<GetManifestTemplateQuery>
{
    public GetManifestTemplateValidator()
    {
        RuleFor(x => x.ManifestTemplateId)
            .NotEmpty();
    }
}
