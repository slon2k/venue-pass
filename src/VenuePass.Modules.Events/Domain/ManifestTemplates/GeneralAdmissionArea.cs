using System.Globalization;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class GeneralAdmissionArea : Entity<GeneralAdmissionAreaId>
{
    private GeneralAdmissionArea()
    {
    }

    private GeneralAdmissionArea(
        GeneralAdmissionAreaId id,
        GeneralAdmissionAreaName name,
        GeneralAdmissionCapacity capacity)
        : base(id)
    {
        Name = name;
        Capacity = capacity;
    }

    public GeneralAdmissionAreaName Name { get; private set; } = null!;
    public GeneralAdmissionCapacity Capacity { get; private set; } = null!;

    internal static GeneralAdmissionArea Create(
        GeneralAdmissionAreaName name,
        GeneralAdmissionCapacity capacity) => new(
            GeneralAdmissionAreaId.Create(),
            name,
            capacity);
}

public readonly record struct GeneralAdmissionAreaId(Guid Value)
{
    public static GeneralAdmissionAreaId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(GeneralAdmissionAreaId id) => id.Value;
    public override string ToString() => Value.ToString();
};

public sealed record GeneralAdmissionAreaName
{
    public const int MaxLength = 100;
    public string Value { get; private set; }

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
    public int Value { get; private set; }

    public GeneralAdmissionCapacity(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero.");
        }

        Value = value;
    }

    public static implicit operator int(GeneralAdmissionCapacity capacity) => capacity.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}