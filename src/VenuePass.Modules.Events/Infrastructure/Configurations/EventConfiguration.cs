using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Venues;

namespace VenuePass.Modules.Events.Infrastructure.Configurations;

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new EventId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.VenueId)
            .HasConversion(
                id => id.Value,
                value => new VenueId(value))
            .HasColumnName("venue_id")
            .IsRequired();

        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(e => e.VenueId)
            .OnDelete(DeleteBehavior.NoAction);

        // ManifestId is stored as a plain column; no FK constraint because
        // Event and Manifest are created together in one transaction, and
        // enforcing both directions would create a circular FK.
        builder.Property(x => x.ManifestId)
            .HasConversion(
                id => id.Value,
                value => new ManifestId(value))
            .HasColumnName("manifest_id")
            .IsRequired();

        builder.HasIndex(x => x.ManifestId)
            .IsUnique()
            .HasDatabaseName("UX_events_manifest_id");

        builder.Property(x => x.Name)
            .HasConversion(
                name => name.Value,
                value => new EventName(value))
            .HasMaxLength(EventName.MaxLength)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.EventDate)
            .HasColumnName("event_date")
            .IsRequired();

        builder.Property(x => x.Description)
            .HasConversion(
                desc => desc != null ? desc.Value : null,
                value => value != null ? new EventDescription(value) : null)
            .HasMaxLength(EventDescription.MaxLength)
            .HasColumnName("description")
            .IsRequired(false);

        builder.Property(x => x.State)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("state")
            .IsRequired();

        builder.HasIndex(x => new { x.VenueId, x.EventDate })
            .HasDatabaseName("IX_events_venue_id_event_date");

        builder.Property(x => x.AssignedManagerId)
            .HasConversion(
                id => id.Value,
                value => new UserId(value))
            .HasColumnName("assigned_manager_id")
            .IsRequired();
    }
}
