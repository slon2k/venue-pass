using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Venues;

namespace VenuePass.Modules.Events.Infrastructure.Configurations;

public sealed class ManifestTemplateConfiguration : IEntityTypeConfiguration<ManifestTemplate>
{
    public void Configure(EntityTypeBuilder<ManifestTemplate> builder)
    {
        builder.ToTable("manifest_templates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new ManifestTemplateId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Name)
            .HasConversion(
                name => name.Value,
                value => new ManifestTemplateName(value))
            .HasMaxLength(ManifestTemplateName.MaxLength)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.Description)
            .HasConversion(
                description => description != null ? description.Value : null,
                value => value != null ? new ManifestTemplateDescription(value) : null)
            .HasMaxLength(ManifestTemplateDescription.MaxLength)
            .HasColumnName("description")
            .IsRequired(false);

        builder.Property(x => x.VenueId)
            .HasConversion(
                venueId => venueId.Value,
                value => new VenueId(value))
            .HasColumnName("venue_id")
            .IsRequired();

        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(mt => mt.VenueId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.OwnsMany(mt => mt.GeneralAdmissionAreas, ga =>
        {
            ga.ToTable("manifest_template_general_admission_areas");

            ga.HasKey(ga => ga.Id);            

            ga.Property(ga => ga.Id)
                .HasConversion(
                    id => id.Value,
                    value => new GeneralAdmissionAreaId(value))
                .ValueGeneratedNever()
                .HasColumnName("id");

            ga.WithOwner().HasForeignKey("manifest_template_id");

            ga.Property(ga => ga.Capacity)
                .HasConversion(
                    capacity => capacity.Value,
                    value => new GeneralAdmissionCapacity(value))
                .HasColumnName("capacity")
                .IsRequired();
            
            ga.Property(ga => ga.Name)
                .HasConversion(
                    name => name.Value,
                    value => new GeneralAdmissionAreaName(value))
                .HasMaxLength(GeneralAdmissionAreaName.MaxLength)
                .HasColumnName("name")
                .IsRequired();

        });

        builder.OwnsMany(mt => mt.Sections, s =>
        {
            s.ToTable("manifest_template_sections");

            s.WithOwner().HasForeignKey("manifest_template_id");

            s.HasKey(s => s.Id);
            
            s.Property(s => s.Id)
            .HasConversion(
                id => id.Value,
                value => new SectionId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

            s.Property(s => s.Name)
            .HasConversion(
                name => name.Value,
                value => new SectionName(value))
            .HasMaxLength(SectionName.MaxLength)
            .HasColumnName("name")
            .IsRequired();

            s.OwnsMany(s => s.Rows, r =>
            {
                r.ToTable("manifest_template_section_rows");

                r.WithOwner().HasForeignKey("section_id");

                r.HasKey(r => r.Id);

                r.Property(r => r.Id)
                    .HasConversion(
                        id => id.Value,
                        value => new SeatRowId(value))
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                r.Property(r => r.Label)
                    .HasConversion(
                        label => label.Value,
                        value => new RowLabel(value))
                    .HasMaxLength(RowLabel.MaxLength)
                    .HasColumnName("label")
                    .IsRequired();

                r.OwnsMany(r => r.Seats, seat =>
                {
                    seat.ToTable("manifest_template_section_row_seats");
                    
                    seat.WithOwner().HasForeignKey("row_id");

                    seat.HasKey(seat => seat.Id);

                    seat.Property(seat => seat.Id)
                        .HasConversion(
                            id => id.Value,
                            value => new SeatId(value))
                        .ValueGeneratedNever()
                        .HasColumnName("id");

                    seat.Property(seat => seat.Label)
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