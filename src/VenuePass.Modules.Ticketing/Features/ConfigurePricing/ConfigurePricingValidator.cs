using FluentValidation;

using VenuePass.Modules.Ticketing.Domain.Offers;

namespace VenuePass.Modules.Ticketing.Features.ConfigurePricing;

public sealed class ConfigurePricingValidator : AbstractValidator<ConfigurePricingCommand>
{
    public ConfigurePricingValidator()
    {
        RuleFor(x => x.PriceZones).NotNull();

        RuleForEach(x => x.PriceZones).ChildRules(zone =>
        {
            zone.RuleFor(z => z.Name)
                .NotEmpty()
                .MaximumLength(PriceZoneName.MaxLength);

            zone.RuleFor(z => z.Price)
                .GreaterThan(0)
                .WithMessage("Price must be positive.");

            zone.RuleFor(z => z.SeatIds).NotNull();
            zone.RuleFor(z => z.PoolIds).NotNull();

            zone.RuleFor(z => z)
                .Must(z => (z.SeatIds?.Count ?? 0) > 0 || (z.PoolIds?.Count ?? 0) > 0)
                .WithMessage("Each price zone must target at least one seat or pool.");
        });
    }
}
