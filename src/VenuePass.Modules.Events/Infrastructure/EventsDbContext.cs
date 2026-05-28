using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Messaging;
using VenuePass.Modules.Events.Contracts;
using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Infrastructure.Outbox;

namespace VenuePass.Modules.Events.Infrastructure;

public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    public const string Schema = "events";

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Manifest> Manifests => Set<Manifest>();

    public DbSet<ManifestTemplate> ManifestTemplates => Set<ManifestTemplate>();

    public DbSet<Venue> Venues => Set<Venue>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var integrationEvent = MapToIntegrationEvent(domainEvent);
                if (integrationEvent is null) continue;

                var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());

                OutboxMessages.Add(OutboxMessage.Create(
                    domainEvent.OccurredOn,
                    integrationEvent.GetType().FullName!,
                    payload));
            }

            aggregate.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

#pragma warning disable CA1859 // return type is intentionally IIntegrationEvent? to support future domain events
    private static IIntegrationEvent? MapToIntegrationEvent(DomainEvent domainEvent)
        => domainEvent switch
        {
            EventPublishedDomainEvent e => new EventPublishedIntegrationEvent(
                e.DomainEventId,
                e.EventId.Value,
                e.ManifestId.Value,
                e.OccurredOn),
            _ => null
        };
#pragma warning restore CA1859
}
