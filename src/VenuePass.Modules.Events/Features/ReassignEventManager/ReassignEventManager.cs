using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Infrastructure;

using DomainEvent = VenuePass.Modules.Events.Domain.Events.Event;
using DomainEventId = VenuePass.Modules.Events.Domain.Events.EventId;

namespace VenuePass.Modules.Events.Features.ReassignEventManager;

public sealed record ReassignEventManagerCommand(Guid EventId, Guid NewManagerId);

public sealed class ReassignEventManagerHandler(
    EventsDbContext db,
    IValidator<ReassignEventManagerCommand> validator)
{
    public async Task<Result> Handle(
        ReassignEventManagerCommand command,
        CancellationToken ct)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            return ReassignEventManagerErrors.InvalidData(
                [.. validationResult.Errors.Select(e =>
                    new ValidationErrorDetail(e.PropertyName, e.ErrorMessage))]);
        }

        var eventId = new DomainEventId(command.EventId);

        DomainEvent? @event = await db.Events
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (@event is null)
        {
            return ReassignEventManagerErrors.EventNotFound(command.EventId);
        }

        @event.ReassignManager(new UserId(command.NewManagerId));

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

public sealed class ReassignEventManagerValidator : AbstractValidator<ReassignEventManagerCommand>
{
    public ReassignEventManagerValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.NewManagerId).NotEmpty();
    }
}
