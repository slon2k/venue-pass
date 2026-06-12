using FluentValidation;

using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Offers;

namespace VenuePass.Modules.Ticketing.Features.CreateOffer;

public sealed class CreateOfferValidator : AbstractValidator<CreateOfferCommand>
{
    public CreateOfferValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(OfferName.MaxLength);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(Currency.Length)
            .Matches("^[A-Z]{3}$");

        RuleFor(x => x.SaleEnd)
            .GreaterThan(x => x.SaleStart)
            .When(x => x.SaleStart.HasValue && x.SaleEnd.HasValue)
            .WithMessage("Sale end must be after sale start.");
    }
}
