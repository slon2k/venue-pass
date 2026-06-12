using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Ticketing.Domain.Inventories;

public sealed class InventorySeat : Entity<InventorySeatId>
{
    public Guid SourceSeatId { get; private set; }
    public SectionName Section { get; private set; } = null!;
    public RowLabel Row { get; private set; } = null!;
    public SeatLabel Seat { get; private set; } = null!;
    public SeatAvailability Availability { get; private set; }

    private InventorySeat() { }

    private InventorySeat(
        InventorySeatId id,
        Guid sourceSeatId,
        SectionName section,
        RowLabel row,
        SeatLabel seat,
        SeatAvailability availability) : base(id)
    {
        SourceSeatId = sourceSeatId;
        Section = section;
        Row = row;
        Seat = seat;
        Availability = availability;
    }

    public static InventorySeat Create(
        Guid sourceSeatId,
        string sectionName,
        string rowLabel,
        string seatLabel)
    {
        return new InventorySeat(
            InventorySeatId.Create(),
            sourceSeatId,
            new SectionName(sectionName),
            new RowLabel(rowLabel),
            new SeatLabel(seatLabel),
            SeatAvailability.Available
        );
    }

    internal void Reserve()
    {
        if (Availability != SeatAvailability.Available)
        {
            throw new DomainRuleViolationException(InventoryErrors.SeatNotAvailable(Id));
        }

        Availability = SeatAvailability.Reserved;
    }

    internal void Release()
    {
        if (Availability != SeatAvailability.Reserved)
        {
            throw new DomainRuleViolationException(InventoryErrors.SeatNotReserved(Id));
        }

        Availability = SeatAvailability.Available;
    }

    internal void Sell()
    {
        if (Availability != SeatAvailability.Reserved)
        {
            throw new DomainRuleViolationException(InventoryErrors.SeatNotReserved(Id));
        }

        Availability = SeatAvailability.Sold;
    }

    public bool IsAvailable => Availability == SeatAvailability.Available;

    public bool IsReserved => Availability == SeatAvailability.Reserved;
}

public readonly record struct InventorySeatId(Guid Value)
{
    public static InventorySeatId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(InventorySeatId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
};

public record SectionName
{
    public const int MaxLength = 100;
    public string Value { get; init; }

    public SectionName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(SectionName name) => name.Value;
};

public record RowLabel
{
    public const int MaxLength = 10;
    public string Value { get; init; }

    public RowLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(RowLabel label) => label.Value;
}

public record SeatLabel
{
    public const int MaxLength = 10;
    public string Value { get; init; }

    public SeatLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(SeatLabel label) => label.Value;
}

public enum SeatAvailability
{
    Available = 0,
    Reserved = 1,
    Sold = 2
}