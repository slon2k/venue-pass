using Microsoft.EntityFrameworkCore;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Infrastructure.Outbox;

namespace VenuePass.Modules.Events.Infrastructure;

public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    public const string Schema = "events";

    public DbSet<ManifestTemplate> ManifestTemplates => Set<ManifestTemplate>();

    public DbSet<Venue> Venues => Set<Venue>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);        

        base.OnModelCreating(modelBuilder);
    }
}
