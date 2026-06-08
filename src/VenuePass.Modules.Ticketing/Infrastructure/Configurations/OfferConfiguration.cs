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

        builder.OwnsMany(o => o.PriceLevels, level =>
        {
            level.ToTable("offer_price_levels");

            level.HasKey(l => l.Id);

            level.Property(l => l.Id)
                .HasConversion(
                    id => id.Value,
                    value => new PriceLevelId(value))
                .ValueGeneratedNever()
                .HasColumnName("id");

            level.WithOwner()
                .HasForeignKey("offer_id");

            level.Property(l => l.Name)
                .HasConversion(
                    name => name.Value,
                    value => new PriceLevelName(value))
                .HasMaxLength(PriceLevelName.MaxLength)
                .HasColumnName("name")
                .IsRequired();

            level.HasIndex("offer_id", nameof(PriceLevel.Name))
                .IsUnique()
                .HasDatabaseName("ux_offer_price_levels_offer_id_name");

            level.OwnsMany(l => l.InventorySeatItems, seatItem =>
            {
                seatItem.ToTable("offer_price_level_inventory_seat_items");

                seatItem.WithOwner()
                    .HasForeignKey("price_level_id");

                seatItem.HasKey(
                    "price_level_id",
                    nameof(PriceLevelInventorySeatItem.InventorySeatId));

                seatItem.Property(i => i.InventorySeatId)
                    .HasConversion(
                        id => id.Value,
                        value => new InventorySeatId(value))
                    .HasColumnName("inventory_seat_id")
                    .IsRequired();

                seatItem.Property(i => i.Price)
                    .HasConversion(
                        amount => amount.Value,
                        value => new Amount(value))
                    .HasColumnName("price")
                    .HasPrecision(18, 2)
                    .IsRequired();
            });

            level.OwnsMany(l => l.GeneralAdmissionPoolItems, poolItem =>
            {
                poolItem.ToTable("offer_price_level_general_admission_pool_items");

                poolItem.WithOwner()
                    .HasForeignKey("price_level_id");

                poolItem.HasKey(
                    "price_level_id",
                    nameof(PriceLevelGeneralAdmissionPoolItem.GeneralAdmissionPoolId));

                poolItem.Property(i => i.GeneralAdmissionPoolId)
                    .HasConversion(
                        id => id.Value,
                        value => new GeneralAdmissionPoolId(value))
                    .HasColumnName("general_admission_pool_id")
                    .IsRequired();

                poolItem.Property(i => i.Price)
                    .HasConversion(
                        amount => amount.Value,
                        value => new Amount(value))
                    .HasColumnName("price")
                    .HasPrecision(18, 2)
                    .IsRequired();
            });

            level.Navigation(l => l.InventorySeatItems)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            level.Navigation(l => l.GeneralAdmissionPoolItems)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(o => o.PriceLevels)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
