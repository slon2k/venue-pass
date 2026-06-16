using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.Tickets;

namespace VenuePass.Modules.Ticketing.Infrastructure.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => new TicketId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Ignore(t => t.DomainEvents);

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
            .IsRequired()
            .UseCollation("SQL_Latin1_General_CP1_CI_AS");

        builder.HasIndex(t => t.Code)
            .IsUnique()
            .HasDatabaseName("IX_tickets_ticket_code");

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
    }
}
