namespace VenuePass.Modules.Attendance.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VenuePass.Modules.Attendance.Domain.AttendanceRecords;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;

internal sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records", table =>
        {
            table.HasCheckConstraint(
                "CK_AttendanceRecords_SeatOrPool",
                "((inventory_seat_id IS NOT NULL AND general_admission_pool_id IS NULL) OR (inventory_seat_id IS NULL AND general_admission_pool_id IS NOT NULL))");         
        });

        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new AttendanceRecordId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(x => x.PublishedEventReferenceId)
            .HasConversion(
                id => id.Value,
                value => new PublishedEventReferenceId(value))
            .HasColumnName("published_event_reference_id")
            .IsRequired();

        builder.Property(x => x.TicketId)
            .HasConversion(
                id => id.Value,
                value => new TicketId(value))
            .HasColumnName("ticket_id")
            .IsRequired();

        builder.Property(x => x.TicketCode)
            .HasMaxLength(TicketCode.Length)
            .IsFixedLength()
            .HasConversion(
                code => code.Value,
                value => new TicketCode(value))
            .HasColumnName("ticket_code")
            .IsRequired();

        builder.Property(x => x.CheckedInAt)
            .HasColumnName("checked_in_at")
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasConversion(
                id => id.Value,
                value => new OrderId(value))
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.OrderItemId)
            .HasConversion(
                id => id.Value,
                value => new OrderItemId(value))
            .HasColumnName("order_item_id")
            .IsRequired();

        builder.Property(x => x.InventorySeatId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new InventorySeatId(value.Value) : null)
            .HasColumnName("inventory_seat_id");

        builder.Property(x => x.GeneralAdmissionPoolId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new GeneralAdmissionPoolId(value.Value) : null)
            .HasColumnName("general_admission_pool_id");

        builder.HasIndex(x => x.TicketCode)
            .HasDatabaseName("IX_attendance_records_ticket_code")
            .IsUnique();

        builder.HasIndex(x => x.TicketId)
            .HasDatabaseName("IX_attendance_records_ticket_id")
            .IsUnique();

        builder.HasIndex(x => new { x.PublishedEventReferenceId, x.CheckedInAt })
            .HasDatabaseName("IX_attendance_records_event_checked_in_at");

        builder.HasOne<PublishedEventReference>()
            .WithMany()
            .HasForeignKey(x => x.PublishedEventReferenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}