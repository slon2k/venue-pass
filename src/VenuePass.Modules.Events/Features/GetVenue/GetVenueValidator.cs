using FluentValidation;

namespace VenuePass.Modules.Events.Features.GetVenue;

public sealed class GetVenueValidator : AbstractValidator<GetVenueQuery>
{
    public GetVenueValidator()
    {
        RuleFor(x => x.VenueId)
            .NotEmpty();
    }
}
