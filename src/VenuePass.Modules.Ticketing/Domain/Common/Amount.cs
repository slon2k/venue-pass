using System.Globalization;

using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Ticketing.Domain.Common;

public readonly record struct Amount
{
    public decimal Value { get; }
    public Amount(decimal value)
    {
        value.ThrowIfNotInRange(nameof(value), 0.0m, decimal.MaxValue);
        Value = value;
    }

    public static implicit operator decimal(Amount amount) => amount.Value;
    public override string ToString() => Value.ToString("F2", CultureInfo.InvariantCulture);
}