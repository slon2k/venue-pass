using System.Globalization;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Ticketing.Domain.Inventories;

public sealed class GeneralAdmissionPool : Entity<GeneralAdmissionPoolId>
{
    public Guid SourceAreaId { get; private set; }
    public GeneralAdmissionPoolName Name { get; private set; } = null!;
    public GeneralAdmissionPoolCapacity Capacity { get; private set; } = null!;
    public int AvailableCount { get; private set; }

    private GeneralAdmissionPool() { }

    private GeneralAdmissionPool(
        GeneralAdmissionPoolId id,
        Guid sourceAreaId,
        GeneralAdmissionPoolName name,
        GeneralAdmissionPoolCapacity capacity,
        int availableCount)
        : base(id)
    {
        SourceAreaId = sourceAreaId;
        Name = name;
        Capacity = capacity;
        AvailableCount = availableCount;
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
            capacity: new GeneralAdmissionPoolCapacity(capacity),
            availableCount: capacity
        );
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