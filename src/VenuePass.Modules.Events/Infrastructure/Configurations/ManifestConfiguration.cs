using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Venues;

namespace VenuePass.Modules.Events.Infrastructure.Configurations;

internal sealed class ManifestConfiguration : IEntityTypeConfiguration<Manifest>
{
    public void Configure(EntityTypeBuilder<Manifest> builder)
    {
        builder.ToTable("manifests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new ManifestId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.EventId)
            .HasConversion(
                id => id.Value,
                value => new EventId(value))
            .HasColumnName("event_id")
            .IsRequired();

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(m => m.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.EventId)
            .IsUnique()
            .HasDatabaseName("UX_manifests_event_id");

        builder.Property(x => x.VenueId)
            .HasConversion(
                id => id.Value,
                value => new VenueId(value))
            .HasColumnName("venue_id")
            .IsRequired();

        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(m => m.VenueId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(x => x.Name)
            .HasConversion(
                name => name.Value,
                value => new ManifestName(value))
            .HasMaxLength(ManifestName.MaxLength)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.IsFrozen)
            .HasColumnName("is_frozen")
            .IsRequired();

        builder.OwnsMany(m => m.GeneralAdmissionAreas, ga =>
        {
            ga.ToTable("manifest_general_admission_areas");

            ga.HasKey(x => x.Id);

            ga.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new GeneralAdmissionAreaId(value))
                .ValueGeneratedNever()
                .HasColumnName("id");

            ga.WithOwner().HasForeignKey("manifest_id");

            ga.Property(x => x.Name)
                .HasConversion(
                    name => name.Value,
                    value => new GeneralAdmissionAreaName(value))
                .HasMaxLength(GeneralAdmissionAreaName.MaxLength)
                .HasColumnName("name")
                .IsRequired();

            ga.Property(x => x.Capacity)
                .HasConversion(
                    capacity => capacity.Value,
                    value => new GeneralAdmissionCapacity(value))
                .HasColumnName("capacity")
                .IsRequired();
        });

        builder.OwnsMany(m => m.Sections, s =>
        {
            s.ToTable("manifest_sections");

            s.HasKey(x => x.Id);

            s.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new SectionId(value))
                .ValueGeneratedNever()
                .HasColumnName("id");

            s.WithOwner().HasForeignKey("manifest_id");

            s.Property(x => x.Name)
                .HasConversion(
                    name => name.Value,
                    value => new SectionName(value))
                .HasMaxLength(SectionName.MaxLength)
                .HasColumnName("name")
                .IsRequired();

            s.OwnsMany(s => s.Rows, r =>
            {
                r.ToTable("manifest_section_rows");

                r.HasKey(x => x.Id);

                r.Property(x => x.Id)
                    .HasConversion(
                        id => id.Value,
                        value => new SeatRowId(value))
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                r.WithOwner().HasForeignKey("section_id");

                r.Property(x => x.Label)
                    .HasConversion(
                        label => label.Value,
                        value => new RowLabel(value))
                    .HasMaxLength(RowLabel.MaxLength)
                    .HasColumnName("label")
                    .IsRequired();

                r.OwnsMany(r => r.Seats, seat =>
                {
                    seat.ToTable("manifest_section_row_seats");

                    seat.HasKey(x => x.Id);

                    seat.Property(x => x.Id)
                        .HasConversion(
                            id => id.Value,
                            value => new SeatId(value))
                        .ValueGeneratedNever()
                        .HasColumnName("id");

                    seat.WithOwner().HasForeignKey("row_id");

                    seat.Property(x => x.Label)
                        .HasConversion(
                            label => label.Value,
                            value => new SeatLabel(value))
                        .HasMaxLength(SeatLabel.MaxLength)
                        .HasColumnName("label")
                        .IsRequired();
                });

                r.Navigation(x => x.Seats)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            });

            s.Navigation(x => x.Rows)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(x => x.Sections)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.GeneralAdmissionAreas)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
