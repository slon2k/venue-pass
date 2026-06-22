using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Ticketing.Contracts;

public sealed record TicketValidationResultDto
{
    public bool IsFound { get; }
    public bool IsValid { get; }
    public TicketValidationFailureReason FailureReason { get; }
    public TicketExportDto? Ticket { get; }

    private TicketValidationResultDto(
        bool isFound,
        bool isValid,
        TicketValidationFailureReason failureReason,
        TicketExportDto? ticket)
    {
        if (!Enum.IsDefined(failureReason))
        {
            throw new ArgumentException("Invalid failure reason.", nameof(failureReason));
        }

        if (isValid && !isFound)
        {
            throw new ArgumentException("A valid ticket validation result must be found.");
        }

        if (isValid && ticket is null)
        {
            throw new ArgumentException("A valid ticket validation result must include a ticket.");
        }

        if (!isFound && ticket is not null)
        {
            throw new ArgumentException("A not-found validation result must not include a ticket.");
        }

        if (isValid && failureReason != TicketValidationFailureReason.None)
        {
            throw new ArgumentException("A valid ticket validation result must not include an invalid reason.");
        }

        if (!isValid && failureReason == TicketValidationFailureReason.None)
        {
            throw new ArgumentException("An invalid ticket validation result must include an invalid reason.");
        }

        IsFound = isFound;
        IsValid = isValid;
        FailureReason = failureReason;
        Ticket = ticket;
    }

    public static TicketValidationResultDto CreateForEvent(TicketExportDto ticket, Guid eventId)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        eventId.ThrowIfEmpty(nameof(eventId));

        if (ticket.PublishedEventReferenceId != eventId)
        {
            return new TicketValidationResultDto(
                isFound: true,
                isValid: false,
                failureReason: TicketValidationFailureReason.IncorrectEvent,
                ticket: ticket);
        }

        TicketValidationFailureReason failureReason = ticket.Status switch
        {
            TicketValidationStatus.Issued => TicketValidationFailureReason.None,
            TicketValidationStatus.Canceled => TicketValidationFailureReason.TicketCanceled,
            _ => TicketValidationFailureReason.Other
        };

        return new TicketValidationResultDto(
            isFound: true,
            isValid: ticket.Status == TicketValidationStatus.Issued,
            failureReason: failureReason,
            ticket: ticket);
    }

    public static TicketValidationResultDto TicketNotFound() => new(
        isFound: false,
        isValid: false,
        failureReason: TicketValidationFailureReason.TicketNotFound,
        ticket: null);

    public static TicketValidationResultDto EventNotFound() => new(
        isFound: true,
        isValid: false,
        failureReason: TicketValidationFailureReason.IncorrectEvent,
        ticket: null);

    public static TicketValidationResultDto MalformedTicketCode() => new(
        isFound: false,
        isValid: false,
        failureReason: TicketValidationFailureReason.MalformedTicketCode,
        ticket: null);
}

public enum TicketValidationStatus
{
    Issued = 1,
    Canceled = 2
}

public enum TicketValidationFailureReason
{
    None = 0,
    MalformedTicketCode = 1,
    TicketNotFound = 2,
    TicketCanceled = 3,
    IncorrectEvent = 4,
    Other = 5,
}


public sealed record TicketExportDto
{
    public Guid TicketId { get; }
    public string TicketCode { get; }
    public TicketType TicketType { get; }

    public TicketValidationStatus Status { get; }
    public Guid PublishedEventReferenceId { get; }
    public Guid OrderId { get; }
    public Guid OrderItemId { get; }
    public Guid? InventorySeatId { get; }
    public Guid? GeneralAdmissionPoolId { get; }
    public DateTimeOffset IssuedAt { get; }

    private TicketExportDto(
    Guid ticketId,
    Guid publishedEventReferenceId,
    Guid orderId,
    Guid orderItemId,
    string code,
    TicketType ticketType,
    Guid? inventorySeatId,
    Guid? generalAdmissionPoolId,
    TicketValidationStatus status,
    DateTimeOffset issuedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));

        if (!Enum.IsDefined(ticketType))
        {
            throw new ArgumentException("Invalid ticket type.", nameof(ticketType));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Invalid ticket validation status.", nameof(status));
        }

        if (ticketType == TicketType.ReservedSeating &&
            (!inventorySeatId.HasValue || inventorySeatId.Value == Guid.Empty))
        {
            throw new ArgumentException("InventorySeatId must be provided for reserved seating tickets.", nameof(inventorySeatId));
        }

        if (ticketType == TicketType.GeneralAdmission &&
            (!generalAdmissionPoolId.HasValue || generalAdmissionPoolId.Value == Guid.Empty))
        {
            throw new ArgumentException("GeneralAdmissionPoolId must be provided for general admission tickets.", nameof(generalAdmissionPoolId));
        }

        if (ticketType == TicketType.ReservedSeating && generalAdmissionPoolId is not null)
        {
            throw new ArgumentException("GeneralAdmissionPoolId must be null for reserved seating tickets.", nameof(generalAdmissionPoolId));
        }

        if (ticketType == TicketType.GeneralAdmission && inventorySeatId is not null)
        {
            throw new ArgumentException("InventorySeatId must be null for general admission tickets.", nameof(inventorySeatId));
        }

        if (issuedAt == default)
        {
            throw new ArgumentException("IssuedAt must be provided.", nameof(issuedAt));
        }

        ticketId.ThrowIfEmpty(nameof(ticketId));
        publishedEventReferenceId.ThrowIfEmpty(nameof(publishedEventReferenceId));
        orderId.ThrowIfEmpty(nameof(orderId));
        orderItemId.ThrowIfEmpty(nameof(orderItemId));

        TicketId = ticketId;
        PublishedEventReferenceId = publishedEventReferenceId;
        OrderId = orderId;
        OrderItemId = orderItemId;
        TicketCode = code;
        TicketType = ticketType;
        InventorySeatId = inventorySeatId;
        GeneralAdmissionPoolId = generalAdmissionPoolId;
        Status = status;
        IssuedAt = issuedAt;
    }

    public static TicketExportDto CreateForSeat(
        Guid ticketId,
        Guid publishedEventReferenceId,
        Guid orderId,
        Guid orderItemId,
        string code,
        TicketValidationStatus status,
        Guid inventorySeatId,
        DateTimeOffset issuedAt)
    {
        inventorySeatId.ThrowIfEmpty(nameof(inventorySeatId));

        return new TicketExportDto(
            ticketId: ticketId,
            publishedEventReferenceId: publishedEventReferenceId,
            orderId: orderId,
            orderItemId: orderItemId,
            code: code,
            ticketType: TicketType.ReservedSeating,
            inventorySeatId: inventorySeatId,
            generalAdmissionPoolId: null,
            status: status,
            issuedAt: issuedAt);
    }

    public static TicketExportDto CreateForGeneralAdmission(
        Guid ticketId,
        Guid publishedEventReferenceId,
        Guid orderId,
        Guid orderItemId,
        string code,
        Guid generalAdmissionPoolId,
        TicketValidationStatus status,
        DateTimeOffset issuedAt)
    {
        generalAdmissionPoolId.ThrowIfEmpty(nameof(generalAdmissionPoolId));

        return new TicketExportDto(
            ticketId: ticketId,
            publishedEventReferenceId: publishedEventReferenceId,
            orderId: orderId,
            orderItemId: orderItemId,
            code: code,
            ticketType: TicketType.GeneralAdmission,
            inventorySeatId: null,
            generalAdmissionPoolId: generalAdmissionPoolId,
            status: status,
            issuedAt: issuedAt);
    }
}

public enum TicketType
{
    GeneralAdmission = 1,
    ReservedSeating = 2
}
