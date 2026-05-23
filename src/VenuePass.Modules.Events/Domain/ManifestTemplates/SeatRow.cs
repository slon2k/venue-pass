using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class SeatRow : Entity<SeatRowId>
{
    private readonly List<Seat> _seats = [];

    private SeatRow()
    {
    }

    private SeatRow(SeatRowId id, RowLabel label) : base(id)
    {
        Label = label;
    }

    public RowLabel Label { get; private set; } = null!;

    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();

    internal static SeatRow Create(RowLabel label, IEnumerable<SeatDraft> seatDrafts)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(seatDrafts);

        var row = new SeatRow(SeatRowId.Create(), label);

        foreach (var seatDraft in seatDrafts)
        {
            row.AddSeat(seatDraft);
        }

        if (row.Seats.Count == 0)
        {
            throw new DomainRuleViolationException(ManifestTemplateErrors.RowMustContainSeats(label.Value));
        }

        return row;
    }

    private void AddSeat(SeatDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var label = new SeatLabel(draft.Label);

        if (_seats.Any(x => HasSameLabel(x.Label.Value, label.Value)))
        {
            throw new DomainRuleViolationException(ManifestTemplateErrors.DuplicateSeatLabel(label.Value, Label.Value));
        }

        _seats.Add(Seat.Create(label));
    }

    private static bool HasSameLabel(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}

public readonly record struct SeatRowId(Guid Value)
{
    public static SeatRowId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(SeatRowId id) => id.Value;
    public override string ToString() => Value.ToString();
};

public sealed record RowLabel
{
    public const int MaxLength = 10;
    public string Value { get; private set; }

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