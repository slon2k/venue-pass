using System.Globalization;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Ticketing.Domain.Common;

namespace VenuePass.Modules.Ticketing.Domain.Inventories;

public sealed class GeneralAdmissionPool : Entity<GeneralAdmissionPoolId>
{
    public Guid SourceAreaId { get; private set; }
    public GeneralAdmissionPoolName Name { get; private set; } = null!;
    public GeneralAdmissionPoolCapacity Capacity { get; private set; } = null!;

    public int ReservedCount { get; private set; }

    public int SoldCount { get; private set; }

    public int AvailableCount => Capacity.Value - ReservedCount - SoldCount;

    private GeneralAdmissionPool() { }

    private GeneralAdmissionPool(
        GeneralAdmissionPoolId id,
        Guid sourceAreaId,
        GeneralAdmissionPoolName name,
        GeneralAdmissionPoolCapacity capacity)
        : base(id)
    {
        SourceAreaId = sourceAreaId;
        Name = name;
        Capacity = capacity;
        ReservedCount = 0;
        SoldCount = 0;
    }

    public static GeneralAdmissionPool Create(
        Guid sourceAreaId,
        string name,
        int capacity)
    {
        return new GeneralAdmissionPool(
            id: GeneralAdmissionPoolId.Create(),
            sourceAreaId: sourceAreaId,
            name: new GeneralAdmissionPoolName(name),
            capacity: new GeneralAdmissionPoolCapacity(capacity)
        );
    }

    internal void Reserve(Quantity quantity)
    {
        if (quantity.Value <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        if (quantity.Value > AvailableCount)
        {
            throw new DomainConflictException(
                InventoryErrors.NotEnoughGeneralAdmissionPoolCapacity(Id, quantity.Value, AvailableCount));
        }

        ReservedCount += quantity.Value;
    }

    internal void Release(Quantity quantity)
    {
        if (quantity.Value <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        if (quantity.Value > ReservedCount)
        {
            throw new DomainConflictException(
                InventoryErrors.NotEnoughReservedGeneralAdmissionPoolQuantity(
                    Id,
                    quantity.Value,
                    ReservedCount));
        }

        ReservedCount -= quantity.Value;
    }

    internal void Sell(Quantity quantity)
    {
        if (quantity.Value <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        if (quantity.Value > ReservedCount)
        {
            throw new DomainConflictException(
                InventoryErrors.NotEnoughGeneralAdmissionPoolCapacity(Id, quantity.Value, ReservedCount));
        }

        SoldCount += quantity.Value;
        ReservedCount -= quantity.Value;
    }
}

public readonly record struct GeneralAdmissionPoolId(Guid Value)
{
    public static GeneralAdmissionPoolId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(GeneralAdmissionPoolId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
};

public sealed record GeneralAdmissionPoolName
{
    public const int MaxLength = 100;
    public string Value { get; init; }

    public GeneralAdmissionPoolName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(GeneralAdmissionPoolName name) => name.Value;

    public override string ToString() => Value;
}

public sealed record GeneralAdmissionPoolCapacity
{
    public int Value { get; private set; }

    public GeneralAdmissionPoolCapacity(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero.");
        }

        Value = value;
    }

    public static implicit operator int(GeneralAdmissionPoolCapacity capacity) => capacity.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}