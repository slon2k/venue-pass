namespace VenuePass.Modules.Attendance.Domain.Common;

public readonly record struct InventoryId(Guid Value)
{
        public bool IsEmpty => Value == Guid.Empty;   
        public static implicit operator Guid(InventoryId id) => id.Value;
        public override string ToString() => Value.ToString();
}

public readonly record struct InventorySeatId(Guid Value)
{
        public bool IsEmpty => Value == Guid.Empty;   
        public static implicit operator Guid(InventorySeatId id) => id.Value;
        public override string ToString() => Value.ToString();
}

public readonly record struct GeneralAdmissionPoolId(Guid Value)
{
        public bool IsEmpty => Value == Guid.Empty;   
        public static implicit operator Guid(GeneralAdmissionPoolId id) => id.Value;
        public override string ToString() => Value.ToString();
}