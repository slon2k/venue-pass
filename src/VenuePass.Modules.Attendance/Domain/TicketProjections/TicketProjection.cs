using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;

namespace VenuePass.Modules.Attendance.Domain.TicketProjections;

public sealed class TicketProjection : Entity<TicketId>
{
    public TicketCode TicketCode { get; private set; }

    public TicketProjectionStatus Status { get; private set; }

    public PublishedEventReferenceId PublishedEventReferenceId { get; private set; }

    public OrderId OrderId { get; private set; }

    public OrderItemId OrderItemId { get; private set; }

    public InventoryId InventoryId { get; private set; }

    public InventorySeatId? InventorySeatId { get; private set; }

    public GeneralAdmissionPoolId? GeneralAdmissionPoolId { get; private set; }

    public DateTimeOffset LastUpdatedAt { get; private set; }

    private TicketProjection()
    {
    }

    private TicketProjection(
        TicketId id,
        TicketCode ticketCode,
        TicketProjectionStatus status,
        PublishedEventReferenceId publishedEventReferenceId,
        OrderId orderId,
        OrderItemId orderItemId,
        InventoryId inventoryId,
        InventorySeatId? inventorySeatId,
        GeneralAdmissionPoolId? generalAdmissionPoolId,
        DateTimeOffset lastUpdatedAt) : base(id)
    {
        if (id.IsEmpty)
            throw new ArgumentException("Ticket ID cannot be empty.", nameof(id));

        if (ticketCode.IsEmpty)
            throw new ArgumentException("Ticket code cannot be empty.", nameof(ticketCode));

        if (publishedEventReferenceId.IsEmpty)
            throw new ArgumentException("Published Event Reference ID cannot be empty.", nameof(publishedEventReferenceId));

        if (orderId.IsEmpty)
            throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));

        if (orderItemId.IsEmpty)
            throw new ArgumentException("Order Item ID cannot be empty.", nameof(orderItemId));

        if (inventoryId.IsEmpty)
            throw new ArgumentException("Inventory ID cannot be empty.", nameof(inventoryId));

        if (lastUpdatedAt == default)
            throw new ArgumentException("Last updated timestamp cannot be the default value.", nameof(lastUpdatedAt));

        if (inventorySeatId.HasValue && inventorySeatId.Value.IsEmpty)
            throw new ArgumentException("Inventory Seat ID cannot be empty when provided.", nameof(inventorySeatId));

        if (generalAdmissionPoolId.HasValue && generalAdmissionPoolId.Value.IsEmpty)
            throw new ArgumentException("General Admission Pool ID cannot be empty when provided.", nameof(generalAdmissionPoolId));

        if (inventorySeatId.HasValue && generalAdmissionPoolId.HasValue)
            throw new ArgumentException("A ticket cannot be associated with both an inventory seat and a general admission pool.");

        if (!inventorySeatId.HasValue && !generalAdmissionPoolId.HasValue)
            throw new ArgumentException("A ticket must be associated with either an inventory seat or a general admission pool.");

        if (!Enum.IsDefined(status))
            throw new ArgumentException("Invalid ticket projection status.", nameof(status));   

        TicketCode = ticketCode;
        Status = status;
        PublishedEventReferenceId = publishedEventReferenceId;
        OrderId = orderId;
        OrderItemId = orderItemId;
        InventoryId = inventoryId;
        InventorySeatId = inventorySeatId;
        GeneralAdmissionPoolId = generalAdmissionPoolId;
        LastUpdatedAt = lastUpdatedAt;
    }

    public static TicketProjection Create(
        TicketId id,
        TicketCode ticketCode,
        PublishedEventReferenceId publishedEventReferenceId,
        OrderId orderId,
        OrderItemId orderItemId,
        InventoryId inventoryId,
        InventorySeatId? inventorySeatId,
        GeneralAdmissionPoolId? generalAdmissionPoolId,
        DateTimeOffset issuedAt) => new(
            id,
            ticketCode,
            TicketProjectionStatus.Issued,
            publishedEventReferenceId,
            orderId,
            orderItemId,
            inventoryId,
            inventorySeatId,
            generalAdmissionPoolId,
            issuedAt);

    public bool Cancel(DateTimeOffset canceledAt)
    {
        if (canceledAt == default)
            throw new ArgumentException("Canceled timestamp cannot be the default value.", nameof(canceledAt));

        if (LastUpdatedAt > canceledAt)
            return false;

        if (Status == TicketProjectionStatus.Canceled)
            return false;

        Status = TicketProjectionStatus.Canceled;
        LastUpdatedAt = canceledAt;
        return true;
    }
}

public enum TicketProjectionStatus
{
    Issued = 1,
    Canceled = 2
}