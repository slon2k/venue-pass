namespace VenuePass.Modules.Ticketing.Domain.Common;

public sealed record Currency
{
    public const int Length = 3;

    public string Value { get; }

    public Currency(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim().ToUpperInvariant();

        if (value.Length != Length || value.Any(c => c is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Currency must be a 3-letter ISO currency code.",
                nameof(value));
        }

        Value = value;
    }

    public static implicit operator string(Currency currency) => currency.Value;

    public override string ToString() => Value;

    public static readonly Currency USD = new("USD");
    public static readonly Currency EUR = new("EUR");
    public static readonly Currency GBP = new("GBP");
    public static readonly Currency JPY = new("JPY");
}