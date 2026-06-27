using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;

namespace VenuePass.Modules.Attendance.Domain.AttendanceRecords;

public sealed class AttendanceRecord : AggregateRoot<AttendanceRecordId>
{

    public TicketId TicketId { get; private set; }

    public TicketCode TicketCode { get; private set; }

    public PublishedEventReferenceId PublishedEventReferenceId { get; private set; }

    public DateTimeOffset CheckedInAt { get; private set; }

    public OrderId OrderId { get; private set; }

    public OrderItemId OrderItemId { get; private set; }

    public InventorySeatId? InventorySeatId { get; private set; }

    public GeneralAdmissionPoolId? GeneralAdmissionPoolId { get; private set; }

    private AttendanceRecord() { }

    private AttendanceRecord(
        AttendanceRecordId id,
        TicketId ticketId,
        TicketCode ticketCode,
        PublishedEventReferenceId publishedEventId,
        DateTimeOffset checkedInAt,
        OrderId orderId,
        OrderItemId orderItemId,
        InventorySeatId? inventorySeatId,
        GeneralAdmissionPoolId? gaPoolId) : base(id)
    {
        if (id.IsEmpty)
            throw new ArgumentException("Attendance record ID cannot be empty.", nameof(id));

        if (ticketId.IsEmpty)
            throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));

        if (publishedEventId.IsEmpty)
            throw new ArgumentException("Published Event ID cannot be empty.", nameof(publishedEventId));

        if (ticketCode.IsEmpty)
            throw new ArgumentException("Ticket code cannot be empty.", nameof(ticketCode));
        
        if (checkedInAt == default)
            throw new ArgumentException("Checked-in timestamp cannot be the default value.", nameof(checkedInAt));

        if (orderId.IsEmpty)
            throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));

        if (orderItemId.IsEmpty)
            throw new ArgumentException("Order Item ID cannot be empty.", nameof(orderItemId));

        if (inventorySeatId.HasValue && inventorySeatId.Value.IsEmpty)
            throw new ArgumentException("Inventory Seat ID cannot be empty when provided.", nameof(inventorySeatId));

        if (gaPoolId.HasValue && gaPoolId.Value.IsEmpty)
            throw new ArgumentException("General Admission Pool ID cannot be empty when provided.", nameof(gaPoolId));

        if (inventorySeatId.HasValue && gaPoolId.HasValue)
            throw new DomainRuleViolationException(AttendanceRecordErrors.InvalidAttendanceAssociation(inventorySeatId.Value, gaPoolId.Value));

        if (!inventorySeatId.HasValue && !gaPoolId.HasValue)
            throw new DomainRuleViolationException(AttendanceRecordErrors.MissingAttendanceAssociation());
    
        TicketId = ticketId;
        TicketCode = ticketCode;
        PublishedEventReferenceId = publishedEventId;
        CheckedInAt = checkedInAt;
        OrderId = orderId;
        OrderItemId = orderItemId;
        InventorySeatId = inventorySeatId;
        GeneralAdmissionPoolId = gaPoolId;
    }

    public static AttendanceRecord CreateForSeat(
        TicketId ticketId,
        TicketCode ticketCode,
        PublishedEventReferenceId publishedEventId,
        InventorySeatId inventorySeatId,
        DateTimeOffset checkedInAt,
        OrderId orderId,
        OrderItemId orderItemId
        )
    {
        return new AttendanceRecord(
            id: AttendanceRecordId.Create(),
            ticketId: ticketId,
            ticketCode: ticketCode,
            publishedEventId: publishedEventId,
            checkedInAt: checkedInAt,
            orderId: orderId,
            orderItemId: orderItemId,
            inventorySeatId: inventorySeatId,
            gaPoolId: null);
    }

    public static AttendanceRecord CreateForGeneralAdmission(
        TicketId ticketId,
        TicketCode ticketCode,
        PublishedEventReferenceId publishedEventId,
        GeneralAdmissionPoolId gaPoolId,
        DateTimeOffset checkedInAt,
        OrderId orderId,
        OrderItemId orderItemId)
    {
        return new AttendanceRecord(
            id: AttendanceRecordId.Create(),
            ticketId: ticketId,
            ticketCode: ticketCode,
            publishedEventId: publishedEventId,
            checkedInAt: checkedInAt,
            orderId: orderId,
            orderItemId: orderItemId,
            inventorySeatId: null,
            gaPoolId: gaPoolId);
    }
}

public readonly record struct AttendanceRecordId(Guid Value)
{
        public static AttendanceRecordId Create() => new(Guid.CreateVersion7());
        public bool IsEmpty => Value == Guid.Empty;   
        public static implicit operator Guid(AttendanceRecordId id) => id.Value;
        public override string ToString() => Value.ToString();
}


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
