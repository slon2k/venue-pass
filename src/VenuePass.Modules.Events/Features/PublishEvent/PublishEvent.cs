using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Infrastructure;

using DomainEvent = VenuePass.Modules.Events.Domain.Events.Event;
using DomainEventId = VenuePass.Modules.Events.Domain.Events.EventId;

namespace VenuePass.Modules.Events.Features.PublishEvent;

public sealed record PublishEventCommand(Guid EventId, Guid CallerId);

public sealed class PublishEventHandler(
    EventsDbContext db,
    TimeProvider timeProvider)
{
    public async Task<Result> Handle(
        PublishEventCommand command,
        CancellationToken ct)
    {
        var eventId = new DomainEventId(command.EventId);

        DomainEvent? @event = await db.Events
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (@event is null)
        {
            return PublishEventErrors.EventNotFound(command.EventId);
        }

        if ((Guid)@event.AssignedManagerId != command.CallerId)
        {
            return PublishEventErrors.CallerIsNotAssignedManager();
        }

        try
        {
            @event.Publish(timeProvider);
        }
        catch (DomainRuleViolationException ex)
        {
            return Result.Failure(Error.Conflict(ex.Code, ex.Message));
        }

        Manifest? manifest = await db.Manifests
            .FirstOrDefaultAsync(m => m.Id == @event.ManifestId, ct);

        manifest?.Freeze();

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
