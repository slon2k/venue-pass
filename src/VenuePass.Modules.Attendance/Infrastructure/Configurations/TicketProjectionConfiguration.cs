namespace VenuePass.Modules.Attendance.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.TicketProjections;

public class TicketProjectionConfiguration : IEntityTypeConfiguration<TicketProjection>
{
    public void Configure(EntityTypeBuilder<TicketProjection> builder)
    {
        builder.ToTable("ticket_projections", table =>
        {
            table.HasCheckConstraint(
                "CK_ticket_projections_seat_or_pool",
                "((inventory_seat_id IS NOT NULL AND general_admission_pool_id IS NULL) OR " +
                "(inventory_seat_id IS NULL AND general_admission_pool_id IS NOT NULL))");
        });

        builder.HasKey(t => t.Id)
            .HasName("PK_ticket_projections");

        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => new TicketId(value))
            .ValueGeneratedNever()
            .HasColumnName("ticket_id");

        builder.Property(t => t.TicketCode)
            .HasConversion(
                code => code.Value,
                value => new TicketCode(value))
            .HasMaxLength(TicketCode.Length)
            .IsFixedLength()
            .IsRequired()
            .HasColumnName("ticket_code");

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();


        builder.Property(t => t.PublishedEventReferenceId)
            .HasConversion(
                id => id.Value,
                value => new PublishedEventReferenceId(value))
            .HasColumnName("published_event_reference_id")
            .IsRequired();

        builder.Property(t => t.OrderId)
            .HasConversion(
                id => id.Value,
                value => new OrderId(value))
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(t => t.OrderItemId)
            .HasConversion(
                id => id.Value,
                value => new OrderItemId(value))
            .HasColumnName("order_item_id")
            .IsRequired();

        builder.Property(t => t.InventoryId)
            .HasConversion(
                id => id.Value,
                value => new InventoryId(value))
            .HasColumnName("inventory_id")
            .IsRequired();

        builder.Property(t => t.InventorySeatId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new InventorySeatId(value.Value) : null)
            .HasColumnName("inventory_seat_id");

        builder.Property(t => t.GeneralAdmissionPoolId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new GeneralAdmissionPoolId(value.Value) : null)
            .HasColumnName("general_admission_pool_id");

        builder.Property(t => t.LastUpdatedAt)
            .HasColumnName("last_updated_at")
            .IsRequired();

        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .HasColumnName("row_version");

        builder.HasOne<PublishedEventReference>()
            .WithMany()
            .HasForeignKey(t => t.PublishedEventReferenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TicketCode)
            .IsUnique()
            .HasDatabaseName("IX_ticket_projections_ticket_code");

        builder.HasIndex(t => new { t.PublishedEventReferenceId, t.Status })
            .HasDatabaseName("IX_ticket_projections_published_event_reference_id_status");
    }
}