using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Contracts.IntegrationEvents;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Infrastructure;
using VenuePass.Modules.Events.Infrastructure.Outbox;

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

        Manifest? manifest = await db.Manifests.FindAsync([@event.ManifestId], ct);

        if (manifest is null)
        {
            return PublishEventErrors.ManifestNotFound(@event.ManifestId.Value);
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

        manifest.Freeze();

        var integrationEvent = new EventPublishedIntegrationEvent(
            MessageId: Guid.CreateVersion7(),
            EventId: @event.Id,
            ManifestId: @event.ManifestId,
            OccurredOn: timeProvider.GetUtcNow());

        db.OutboxMessages.Add(OutboxMessage.Create(
            occurredOn: integrationEvent.OccurredOn,
            type: integrationEvent.GetType().AssemblyQualifiedName!,
            payload: JsonSerializer.Serialize(integrationEvent)));

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
