using System.Globalization;

namespace VenuePass.Modules.Ticketing.Domain.Common;

public readonly record struct Quantity
{
    public int Value { get; }
    public Quantity(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.",
                nameof(value));
        }

        Value = value;
    }

    public static implicit operator int(Quantity quantity) => quantity.Value;
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}