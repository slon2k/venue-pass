using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;

namespace VenuePass.Modules.Ticketing.Infrastructure;

public sealed class TicketingDbContext(DbContextOptions<TicketingDbContext> options) : DbContext(options)
{
    public const string Schema = "ticketing";

    public DbSet<PublishedEventReference> PublishedEventReferences => Set<PublishedEventReference>();

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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}