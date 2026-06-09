using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;

namespace VenuePass.Modules.Ticketing.Infrastructure.Configurations;

internal sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("offers");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(
                id => id.Value,
                value => new OfferId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Ignore(o => o.DomainEvents);

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

        builder.Property(o => o.Name)
            .HasConversion(
                name => name.Value,
                value => new OfferName(value))
            .HasMaxLength(OfferName.MaxLength)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(o => o.Currency)
            .HasConversion(
                currency => currency.Value,
                value => new Currency(value))
            .HasMaxLength(Currency.Length)
            .IsFixedLength()
            .HasColumnName("currency")
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasColumnName("status")
            .IsRequired();

        builder.ComplexProperty(o => o.SalesRange, range =>
        {
            range.Property(r => r.Start)
                .HasColumnName("sales_start");

            range.Property(r => r.End)
                .HasColumnName("sales_end");
        });

        builder.OwnsMany(o => o.PriceZones, zone =>
        {
            zone.ToTable("offer_price_zones");

            zone.HasKey(z => z.Id);

            zone.Property(z => z.Id)
                .HasConversion(
                    id => id.Value,
                    value => new PriceZoneId(value))
                .ValueGeneratedNever()
                .HasColumnName("id");

            zone.WithOwner()
                .HasForeignKey("offer_id");

            zone.Property(z => z.Name)
                .HasConversion(
                    name => name.Value,
                    value => new PriceZoneName(value))
                .HasMaxLength(PriceZoneName.MaxLength)
                .HasColumnName("name")
                .IsRequired();

            zone.Property(z => z.Price)
                .HasConversion(
                    amount => amount.Value,
                    value => new Amount(value))
                .HasColumnName("price")
                .HasPrecision(18, 2)
                .IsRequired();

            zone.HasIndex("offer_id", nameof(PriceZone.Name))
                .IsUnique()
                .HasDatabaseName("ux_offer_price_zones_offer_id_name");

            zone.OwnsMany(z => z.InventorySeatItems, seatItem =>
            {
                seatItem.ToTable("offer_price_zone_inventory_seat_items");

                seatItem.WithOwner()
                    .HasForeignKey("price_zone_id");

                seatItem.HasKey(
                    "price_zone_id",
                    nameof(PriceZoneInventorySeatItem.InventorySeatId));

                seatItem.Property(i => i.InventorySeatId)
                    .HasConversion(
                        id => id.Value,
                        value => new InventorySeatId(value))
                    .HasColumnName("inventory_seat_id")
                    .IsRequired();

                seatItem.HasIndex(i => i.InventorySeatId);

            });

            zone.OwnsMany(z => z.GeneralAdmissionPoolItems, poolItem =>
            {
                poolItem.ToTable("offer_price_zone_general_admission_pool_items");

                poolItem.WithOwner()
                    .HasForeignKey("price_zone_id");

                poolItem.HasKey(
                    "price_zone_id",
                    nameof(PriceZoneGeneralAdmissionPoolItem.GeneralAdmissionPoolId));

                poolItem.Property(i => i.GeneralAdmissionPoolId)
                    .HasConversion(
                        id => id.Value,
                        value => new GeneralAdmissionPoolId(value))
                    .HasColumnName("general_admission_pool_id")
                    .IsRequired();

                poolItem.HasIndex(i => i.GeneralAdmissionPoolId);

            });

            zone.Navigation(z => z.InventorySeatItems)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            zone.Navigation(z => z.GeneralAdmissionPoolItems)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(o => o.PriceZones)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
