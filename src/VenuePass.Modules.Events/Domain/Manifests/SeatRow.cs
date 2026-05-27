using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

using TemplateModel = VenuePass.Modules.Events.Domain.ManifestTemplates;

namespace VenuePass.Modules.Events.Domain.Manifests;

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

    internal static SeatRow CreateFrom(TemplateModel.SeatRow templateRow)
    {
        ArgumentNullException.ThrowIfNull(templateRow);

        var row = new SeatRow(SeatRowId.Create(), new RowLabel(templateRow.Label));

        foreach (var templateSeat in templateRow.Seats)
        {
            row._seats.Add(Seat.Create(new SeatLabel(templateSeat.Label)));
        }

        if (row.Seats.Count == 0)
        {
            throw new DomainRuleViolationException(ManifestErrors.RowMustContainSeats(row.Label));
        }

        return row;
    }
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