using FluentValidation;

using VenuePass.Modules.Events.Domain.Events;

namespace VenuePass.Modules.Events.Features.CreateEvent;

public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.VenueId)
            .NotEmpty();

        RuleFor(x => x.ManifestTemplateId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(EventName.MaxLength);

        RuleFor(x => x.EventDate)
            .NotEmpty()
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Event date must be in the future.");

        RuleFor(x => x.Description)
            .MaximumLength(EventDescription.MaxLength)
            .When(x => x.Description is not null);
    }
}
