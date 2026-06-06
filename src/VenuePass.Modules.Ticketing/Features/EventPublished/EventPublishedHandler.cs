using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Messaging;
using VenuePass.Modules.Events.Contracts;
using VenuePass.Modules.Events.Contracts.IntegrationEvents;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.EventPublished;

internal sealed class EventPublishedHandler(
    TicketingDbContext db,
    IEventsModuleContract eventsContract,
    TimeProvider timeProvider)
    : IIntegrationEventHandler<EventPublishedIntegrationEvent>
{
    public async Task Handle(EventPublishedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        bool alreadySynced = await db.PublishedEventReferences
            .AnyAsync(reference => reference.EventId == integrationEvent.EventId, cancellationToken);

        if (alreadySynced)
        {
            return;
        }

        var manifest = await eventsContract
            .GetManifestForTicketingAsync(integrationEvent.ManifestId, cancellationToken) ?? throw new InvalidOperationException(
                $"Manifest {integrationEvent.ManifestId} not available for ticketing sync.");

        var reference = PublishedEventReference.Create(
            eventId: integrationEvent.EventId,
            manifestId: integrationEvent.ManifestId,
            syncedAt: timeProvider.GetUtcNow());

        Inventory inventory = Inventory.CreateFromManifest(reference.Id, manifest.ToInventoryManifest());

        db.PublishedEventReferences.Add(reference);
        db.Inventories.Add(inventory);

        await db.SaveChangesAsync(cancellationToken);
    }
}