using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VenuePass.Modules.Events.Domain.Venues;

namespace VenuePass.Modules.Events.Infrastructure.Configurations;

internal sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Name)
            .HasConversion(
                name => name.Value,
                value => new VenueName(value))
            .HasMaxLength(VenueName.MaxLength)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.Capacity)
            .HasConversion(
                capacity => capacity.Value,
                value => new VenueCapacity(value))
            .HasColumnName("capacity")
            .IsRequired();

        builder.OwnsOne(x => x.Address, owned =>
        {
            owned.Property(x => x.StreetAddress)
                .HasConversion(
                    address => address.Value,
                    value => new StreetAddress(value))
                .HasMaxLength(StreetAddress.MaxLength)
                .HasColumnName("address")
                .IsRequired();

            owned.Property(x => x.City)
                .HasConversion(
                    city => city.Value,
                    value => new City(value))
                .HasMaxLength(City.MaxLength)
                .HasColumnName("city")
                .IsRequired();

            owned.Property(x => x.Country)
                .HasConversion(
                    country => country.Value,
                    value => new Country(value))
                .HasMaxLength(Country.MaxLength)
                .HasColumnName("country")
                .IsRequired();
        });
    }
}