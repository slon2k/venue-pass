using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class Seat : Entity<SeatId>
{
    private Seat(
        SeatId id,
        SeatLabel label)
        : base(id)
    {
        Label = label;
    }

    public SeatLabel Label { get; private set; }

    internal static Seat Create(
        SeatId id,
        string label)
    {
        return new Seat(id, new SeatLabel(label));
    }
}

public sealed record SeatId(Guid Value)
{
    public static SeatId New() => new(Guid.NewGuid());
    public static implicit operator Guid(SeatId id) => id.Value;
    public static implicit operator SeatId(Guid value) => new(value);
};

public sealed record SeatLabel
{
    public const int MaxLength = 10;
    public string Value { get; }

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