namespace VenuePass.Modules.Attendance.Domain.Common;

public readonly record struct TicketCode
{
    private const string CrockfordBase32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    public const int Length = 16;
    public string Value { get; }

    public static bool TryCreate(string value, out TicketCode ticketCode)
    {
        ticketCode = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim().Replace("-", "").ToUpperInvariant();

        if (value.Length != Length)
            return false;

        foreach (var c in value)
        {
            if (!CrockfordBase32.Contains(c))
                return false;
        }

        ticketCode = new TicketCode(value);
        return true;
    }

    public TicketCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        value = value.Trim().Replace("-", "").ToUpperInvariant();

        if (value.Length != Length)
        {
            throw new ArgumentException($"Ticket code must be {Length} characters long.", nameof(value));
        }

        foreach (var c in value)            
        {
            if (!CrockfordBase32.Contains(c))
            {
                throw new ArgumentException(
                    $"Ticket code contains invalid character '{c}'. Only Crockford's Base32 characters are allowed.",
                    nameof(value));
            }
        }

        Value = value;
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value ?? string.Empty;
}

public readonly record struct TicketId(Guid Value)
{
        public bool IsEmpty => Value == Guid.Empty;   
        public static implicit operator Guid(TicketId id) => id.Value;
        public override string ToString() => Value.ToString();
}