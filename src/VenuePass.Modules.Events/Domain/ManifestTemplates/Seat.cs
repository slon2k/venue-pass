using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class Seat : Entity<SeatId>
{
    private Seat()
    {
    }

    private Seat(SeatId id, SeatLabel label): base(id)
    {
        Label = label;
    }

    public SeatLabel Label { get; private set; } = null!;

    internal static Seat Create(SeatLabel label)
    {
        ArgumentNullException.ThrowIfNull(label);
        return new(SeatId.Create(), label);
    }
}

public readonly record struct SeatId(Guid Value)
{
    public static SeatId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(SeatId id) => id.Value;
    public override string ToString() => Value.ToString();
};

public sealed record SeatLabel
{
    public const int MaxLength = 10;
    public string Value { get; private set; }

    public SeatLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(SeatLabel label) => label.Value;

    public override string ToString() => Value;
}