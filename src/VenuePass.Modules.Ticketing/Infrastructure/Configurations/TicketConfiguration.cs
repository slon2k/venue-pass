using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Domain.Tickets;

namespace VenuePass.Modules.Ticketing.Infrastructure.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets", TicketingDbContext.Schema, table =>
        {
            table.HasCheckConstraint(
                "CK_tickets_exactly_one_target",
                "([inventory_seat_id] IS NOT NULL AND [general_admission_pool_id] IS NULL) OR " +
                "([inventory_seat_id] IS NULL AND [general_admission_pool_id] IS NOT NULL)");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => new TicketId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Ignore(t => t.DomainEvents);

        builder.Property(t => t.PublishedEventReferenceId)
            .HasConversion(
                id => id.Value,
                value => new PublishedEventReferenceId(value))
            .HasColumnName("published_event_reference_id")
            .IsRequired();

        builder.Property(t => t.InventoryId)
            .HasConversion(
                id => id.Value,
                value => new InventoryId(value))
            .HasColumnName("inventory_id")
            .IsRequired();

        builder.Property(t => t.OrderId)
            .HasConversion(
                id => id.Value,
                value => new OrderId(value))
            .HasColumnName("order_id")
            .IsRequired();

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.OrderItemId)
            .HasConversion(
                id => id.Value,
                value => new OrderItemId(value))
            .HasColumnName("order_item_id")
            .IsRequired();

        builder.Property(t => t.Code)
            .HasConversion(
                code => code.Value,
                value => new TicketCode(value))
            .HasMaxLength(TicketCode.Length)
            .IsFixedLength()
            .HasColumnName("ticket_code")
            .IsRequired();

        builder.HasIndex(t => t.Code)
            .IsUnique()
            .HasDatabaseName("IX_tickets_ticket_code");

        builder.HasIndex(t => t.InventoryId)
            .HasDatabaseName("IX_tickets_inventory_id");

        builder.Property(t => t.InventorySeatId)
            .HasConversion(
                id => id.HasValue ? (Guid?)id.Value.Value : null,
                value => value.HasValue ? new InventorySeatId(value.Value) : (InventorySeatId?)null)
            .HasColumnName("inventory_seat_id");

        builder.Property(t => t.GeneralAdmissionPoolId)
            .HasConversion(
                id => id.HasValue ? (Guid?)id.Value.Value : null,
                value => value.HasValue ? new GeneralAdmissionPoolId(value.Value) : (GeneralAdmissionPoolId?)null)
            .HasColumnName("general_admission_pool_id");

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.CanceledAt)
            .HasColumnName("canceled_at")
            .IsRequired(false);

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .HasColumnName("row_version");
    }
}
