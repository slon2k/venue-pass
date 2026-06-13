using FluentValidation;

namespace VenuePass.Modules.Ticketing.Features.CreateReservation;

public sealed class CreateReservationValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();

        RuleFor(x => x.SeatIds).NotNull();
        RuleForEach(x => x.SeatIds).NotEmpty();

        RuleFor(x => x.GeneralAdmissionPoolSelections).NotNull();
        RuleForEach(x => x.GeneralAdmissionPoolSelections).ChildRules(pool =>
        {
            pool.RuleFor(p => p.PoolId).NotEmpty();
            pool.RuleFor(p => p.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");
        });

RuleFor(x => x)
    .Must(x =>
        (x.SeatIds?.Count ?? 0) > 0 ||
        (x.GeneralAdmissionPoolSelections?.Count ?? 0) > 0)
    .WithMessage("Reservation must contain at least one seat or general admission pool selection.");
    }
}