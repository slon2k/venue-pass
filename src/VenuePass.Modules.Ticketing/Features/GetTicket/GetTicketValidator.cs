using FluentValidation;

namespace VenuePass.Modules.Ticketing.Features.GetTicket;

public sealed class GetTicketValidator : AbstractValidator<GetTicketQuery>
{
    public GetTicketValidator()
    {
        RuleFor(x => x.TicketCode)
            .NotEmpty()
            .WithMessage("Ticket code is required.");
    }
}