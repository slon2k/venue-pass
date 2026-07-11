using VenuePass.BuildingBlocks.Messaging;

namespace VenuePass.Modules.Attendance.Contracts;

public sealed record TicketCheckedInIntegrationEvent(
    Guid MessageId,
    Guid TicketId,
    string TicketCode,
    Guid PublishedEventId,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    Guid OrderId,
    Guid OrderItemId,
    DateTimeOffset OccurredOn
) : IIntegrationEvent;