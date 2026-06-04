using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Domain;
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
            .Select(e => e.Entity);

        foreach (var aggregate in aggregates)
        {
            // Handling will be implemented in the next steps, just clearing the events for now
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
}
