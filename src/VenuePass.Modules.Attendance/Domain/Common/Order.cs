namespace VenuePass.Modules.Attendance.Domain.Common;

public readonly record struct OrderId(Guid Value)
{
        public bool IsEmpty => Value == Guid.Empty;   
        public static implicit operator Guid(OrderId id) => id.Value;
        public override string ToString() => Value.ToString();
}

public readonly record struct OrderItemId(Guid Value)
{
        public bool IsEmpty => Value == Guid.Empty;   
        public static implicit operator Guid(OrderItemId id) => id.Value;
        public override string ToString() => Value.ToString();
}
