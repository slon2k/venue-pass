using FluentValidation;

namespace VenuePass.Modules.Ticketing.Features.CheckoutReservation;

public sealed class CheckoutReservationValidator : AbstractValidator<CheckoutReservationCommand>
{
    public CheckoutReservationValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();

        RuleFor(x => x.BuyerName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.BuyerEmail)
            .NotEmpty()
            .MaximumLength(254)
            .EmailAddress();
    }
}
