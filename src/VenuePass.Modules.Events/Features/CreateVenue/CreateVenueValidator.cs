using FluentValidation;

using VenuePass.Modules.Events.Domain.Venues;

namespace VenuePass.Modules.Events.Features.CreateVenue;

public sealed class CreateVenueValidator : AbstractValidator<CreateVenueCommand>
{
    public CreateVenueValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(VenueName.MaxLength);

        RuleFor(x => x.StreetAddress)
            .NotEmpty()
            .MaximumLength(StreetAddress.MaxLength);

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(City.MaxLength);

        RuleFor(x => x.Country)
            .NotEmpty()
            .MaximumLength(Country.MaxLength);

        RuleFor(x => x.Capacity)
            .GreaterThan(0);
    }
}