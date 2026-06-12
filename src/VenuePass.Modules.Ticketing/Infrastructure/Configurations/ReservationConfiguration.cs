using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.Reservations;

namespace VenuePass.Modules.Ticketing.Infrastructure.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(
                id => id.Value,
                value => new ReservationId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Ignore(r => r.DomainEvents);

        builder.Property(r => r.OfferId)
            .HasConversion(
                id => id.Value,
                value => new OfferId(value))
            .HasColumnName("offer_id")
            .IsRequired();

        builder.HasOne<Offer>()
            .WithMany()
            .HasForeignKey(r => r.OfferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.InventoryId)
            .HasConversion(
                id => id.Value,
                value => new InventoryId(value))
            .HasColumnName("inventory_id")
            .IsRequired();

        builder.HasOne<Inventory>()
            .WithMany()
            .HasForeignKey(r => r.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(r => r.Currency)
            .HasConversion(
                currency => currency.Value,
                value => new Currency(value))
            .HasMaxLength(Currency.Length)
            .IsFixedLength()
            .HasColumnName("currency")
            .IsRequired();

        builder.Property(r => r.Total)
            .HasConversion(
                amount => amount.Value,
                value => new Amount(value))
            .HasPrecision(18, 2)
            .HasColumnName("total")
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .HasColumnName("row_version");

        builder.OwnsMany(r => r.Items, item =>
        {
            item.ToTable("reservation_items");

            item.HasKey(i => i.Id);

            item.Property(i => i.Id)
                .HasConversion(
                    id => id.Value,
                    value => new ReservationItemId(value))
                .ValueGeneratedNever()
                .HasColumnName("id");

            item.WithOwner()
                .HasForeignKey("reservation_id");

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

        builder.Navigation(r => r.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
