using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class SeatRow : Entity<SeatRowId>
{
    private readonly List<Seat> _seats = [];

    private SeatRow(
        SeatRowId id,
        RowLabel label)
        : base(id)
    {
        Label = label;
    }

    public RowLabel Label { get; private set; }

    public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();

    internal static SeatRow Create(
        SeatRowId id,
        RowDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var row = new SeatRow(
            id,
            new RowLabel(draft.Label));

        foreach (var seatDraft in draft.Seats)
        {
            row.AddSeat(seatDraft);
        }

        return row;
    }

    private void AddSeat(SeatDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (_seats.Any(x => x.Label == draft.Label))
        {
            throw new ArgumentException(
                $"Seat with label '{draft.Label}' already exists in row '{Label}'.");
        }

        _seats.Add(
            Seat.Create(
                SeatId.New(),
                draft.Label));
    }
}

public sealed record SeatRowId(Guid Value)
{
    public static SeatRowId New() => new(Guid.NewGuid());
    public static implicit operator Guid(SeatRowId id) => id.Value;
    public static implicit operator SeatRowId(Guid value) => new(value);
};

public sealed record RowLabel
{
    public const int MaxLength = 10;
    public string Value { get; }

    public RowLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(RowLabel label) => label.Value;

    public override string ToString() => Value;
}