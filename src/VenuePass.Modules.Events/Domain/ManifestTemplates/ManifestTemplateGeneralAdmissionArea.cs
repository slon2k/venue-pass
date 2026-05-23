using System.Globalization;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class GeneralAdmissionArea : Entity<GeneralAdmissionAreaId>
{
    private GeneralAdmissionArea(
        GeneralAdmissionAreaId id,
        GeneralAdmissionAreaName name,
        GeneralAdmissionCapacity capacity)
        : base(id)
    {
        Name = name;
        Capacity = capacity;
    }

    public GeneralAdmissionAreaName Name { get; private set; }
    public GeneralAdmissionCapacity Capacity { get; private set; }

    internal static GeneralAdmissionArea Create(
        GeneralAdmissionAreaId id,
        string name,
        int capacity)
    {
        return new GeneralAdmissionArea(
            id,
            new GeneralAdmissionAreaName(name),
            new GeneralAdmissionCapacity(capacity));
    }
}

public sealed record GeneralAdmissionAreaId(Guid Value)
{
    public static GeneralAdmissionAreaId New() => new(Guid.NewGuid());
    public static implicit operator Guid(GeneralAdmissionAreaId id) => id.Value;
    public static implicit operator GeneralAdmissionAreaId(Guid value) => new(value);
};

public sealed record GeneralAdmissionAreaName
{
    public const int MaxLength = 100;
    public string Value { get; }

    public GeneralAdmissionAreaName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(GeneralAdmissionAreaName name) => name.Value;

    public override string ToString() => Value;
}

public sealed record GeneralAdmissionCapacity
{
    public int Value { get; }

    public GeneralAdmissionCapacity(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity cannot be negative.");
        }

        Value = value;
    }

    public static implicit operator int(GeneralAdmissionCapacity capacity) => capacity.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}