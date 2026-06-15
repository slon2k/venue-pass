using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.Reservations;

namespace VenuePass.Modules.Ticketing.Infrastructure.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(
                id => id.Value,
                value => new OrderId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Ignore(o => o.DomainEvents);

        builder.Property(o => o.ReservationId)
            .HasConversion(
                id => id.Value,
                value => new ReservationId(value))
            .HasColumnName("reservation_id")
            .IsRequired();

        builder.HasOne<Reservation>()
            .WithMany()
            .HasForeignKey(o => o.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.ReservationId)
            .IsUnique();

        builder.Property(o => o.OfferId)
            .HasConversion(
                id => id.Value,
                value => new OfferId(value))
            .HasColumnName("offer_id")
            .IsRequired();

        builder.HasOne<Offer>()
            .WithMany()
            .HasForeignKey(o => o.OfferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.InventoryId)
            .HasConversion(
                id => id.Value,
                value => new InventoryId(value))
            .HasColumnName("inventory_id")
            .IsRequired();

        builder.HasOne<Inventory>()
            .WithMany()
            .HasForeignKey(o => o.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.BuyerName)
            .HasMaxLength(200)
            .HasColumnName("buyer_name")
            .IsRequired();

        builder.Property(o => o.BuyerEmail)
            .HasMaxLength(254)
            .HasColumnName("buyer_email")
            .IsRequired();

        builder.Property(o => o.Currency)
            .HasConversion(
                currency => currency.Value,
                value => new Currency(value))
            .HasMaxLength(Currency.Length)
            .IsFixedLength()
            .HasColumnName("currency")
            .IsRequired();

        builder.Property(o => o.Total)
            .HasConversion(
                amount => amount.Value,
                value => new Amount(value))
            .HasPrecision(18, 2)
            .HasColumnName("total")
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasColumnName("status")
            .IsRequired();

        builder.OwnsMany(o => o.Items, item =>
        {
            item.ToTable("order_items");

            item.HasKey(i => i.Id);

            item.Property(i => i.Id)
                .HasConversion(
                    id => id.Value,
                    value => new OrderItemId(value))
                .ValueGeneratedNever()
                .HasColumnName("id");

            item.WithOwner()
                .HasForeignKey("order_id");

            item.Property(i => i.Type)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasColumnName("type")
                .IsRequired();

            item.Property(i => i.PriceZoneId)
                .HasConversion(
                    id => id.Value,
                    value => new PriceZoneId(value))
                .HasColumnName("price_zone_id")
                .IsRequired();

            item.Property(i => i.InventorySeatId)
                .HasConversion(
                    id => id.HasValue ? (Guid?)id.Value.Value : null,
                    value => value.HasValue ? new InventorySeatId(value.Value) : (InventorySeatId?)null)
                .HasColumnName("inventory_seat_id");

            item.Property(i => i.GeneralAdmissionPoolId)
                .HasConversion(
                    id => id.HasValue ? (Guid?)id.Value.Value : null,
                    value => value.HasValue ? new GeneralAdmissionPoolId(value.Value) : (GeneralAdmissionPoolId?)null)
                .HasColumnName("general_admission_pool_id");

            item.Property(i => i.UnitPrice)
                .HasConversion(
                    amount => amount.Value,
                    value => new Amount(value))
                .HasPrecision(18, 2)
                .HasColumnName("unit_price")
                .IsRequired();

            item.Property(i => i.Quantity)
                .HasConversion(
                    q => q.Value,
                    value => new Quantity(value))
                .HasColumnName("quantity")
                .IsRequired();

            item.Property(i => i.Total)
                .HasConversion(
                    amount => amount.Value,
                    value => new Amount(value))
                .HasPrecision(18, 2)
                .HasColumnName("total")
                .IsRequired();
        });

        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
