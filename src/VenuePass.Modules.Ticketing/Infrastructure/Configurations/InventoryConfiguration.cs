using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;

namespace VenuePass.Modules.Ticketing.Infrastructure.Configurations;

internal sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new InventoryId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.EventReferenceId)
            .HasConversion(
                id => id.Value,
                value => new PublishedEventReferenceId(value))
            .HasColumnName("event_reference_id")
            .IsRequired();

        builder.HasOne<PublishedEventReference>()
            .WithOne()
            .HasForeignKey<Inventory>(i => i.EventReferenceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(i => i.Seats, s =>
        {
            s.ToTable("inventory_seats");

            s.HasKey(s => s.Id);

            s.Property(s => s.Id)
                .HasConversion(
                    id => id.Value,
                    value => new InventorySeatId(value))
                .ValueGeneratedNever()
                .HasColumnName("id");
            
            s.WithOwner()
                .HasForeignKey("inventory_id");

            s.Property(s => s.SourceSeatId)
                .HasColumnName("source_seat_id")
                .IsRequired();

            s.Property(s => s.Section)
                .HasConversion(
                    section => section.Value,
                    value => new SectionName(value))
                .HasMaxLength(SectionName.MaxLength)
                .HasColumnName("section")
                .IsRequired();

            s.Property(s => s.Row)
                .HasConversion(
                    row => row.Value,
                    value => new RowLabel(value))
                .HasMaxLength(RowLabel.MaxLength)
                .HasColumnName("row")
                .IsRequired();

            s.Property(s => s.Seat)
                .HasConversion(
                    seat => seat.Value,
                    value => new SeatLabel(value))
                .HasMaxLength(SeatLabel.MaxLength)
                .HasColumnName("seat")
                .IsRequired();

            s.Property(s => s.Availability)
                .HasConversion<string>()
                .HasColumnName("availability")
                .IsRequired();
        });

        builder.OwnsMany(i => i.Pools, p =>
        {
            p.ToTable("inventory_pools");

            p.HasKey(p => p.Id);

            p.Property(p => p.Id)
                .HasConversion(
                    id => id.Value,
                    value => new GeneralAdmissionPoolId(value))
                .ValueGeneratedNever()
                .HasColumnName("id");
            
            p.WithOwner()
                .HasForeignKey("inventory_id");

            p.Property(p => p.SourceAreaId)
                .HasColumnName("source_area_id")
                .IsRequired();

            p.Property(p => p.Name)
                .HasConversion(
                    name => name.Value,
                    value => new GeneralAdmissionPoolName(value))
                .HasMaxLength(GeneralAdmissionPoolName.MaxLength)
                .HasColumnName("name")
                .IsRequired();

            p.Property(p => p.Capacity)
                .HasConversion(
                    capacity => capacity.Value,
                    value => new GeneralAdmissionPoolCapacity(value))
                .HasColumnName("capacity")
                .IsRequired();

            p.Property(p => p.ReservedCount)
                .HasColumnName("reserved_count")
                .IsRequired();

            p.Property(p => p.SoldCount)
                .HasColumnName("sold_count")
                .IsRequired();

            p.Ignore(p => p.AvailableCount);
        });

        builder.Navigation(i => i.Seats)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(i => i.Pools)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}