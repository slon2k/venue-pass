using VenuePass.BuildingBlocks.Messaging;

namespace VenuePass.Modules.Ticketing.Contracts;

public sealed record TicketIssuedIntegrationEvent(
    Guid MessageId,
    Guid TicketId,
    string TicketCode,
    Guid OrderId,
    Guid OrderItemId,
    Guid PublishedEventReferenceId,
    Guid InventoryId,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    DateTimeOffset OccurredOn
) : IIntegrationEvent;

public sealed record TicketCanceledIntegrationEvent(
    Guid MessageId,
    Guid TicketId,
    string TicketCode,
    Guid PublishedEventReferenceId,
    DateTimeOffset OccurredOn
) : IIntegrationEvent;