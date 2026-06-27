using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VenuePass.Modules.Attendance.Domain.PublishedEvents;

namespace VenuePass.Modules.Attendance.Infrastructure.Configurations;

internal sealed class PublishedEventReferenceConfiguration : IEntityTypeConfiguration<PublishedEventReference>
{
    public void Configure(EntityTypeBuilder<PublishedEventReference> builder)
    {
        builder.ToTable("published_event_references");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new PublishedEventReferenceId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(x => x.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.HasIndex(x => new { x.EventId, x.ManifestId })
            .IsUnique();

        builder.Property(x => x.ManifestId)
            .HasColumnName("manifest_id")
            .IsRequired();

        builder.Property(x => x.SyncedAt)
            .HasColumnName("synced_at")
            .IsRequired();
    }
}