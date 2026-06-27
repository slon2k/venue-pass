using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;

namespace VenuePass.Modules.Attendance.Domain.ScanAttempts;

public sealed class ScanAttempt : AggregateRoot<ScanAttemptId>
{
    public SubmittedTicketCode SubmittedTicketCode { get; private set; } = null!;

    public TicketCode? NormalizedTicketCode { get; private set; }

    public ScanOutcome Outcome { get; private set; }

    public ScanRejectionCategory RejectionCategory { get; private set; }

    public DateTimeOffset ScannedAt { get; private set; }

    public TicketId? TicketId { get; private set; }

    public PublishedEventReferenceId PublishedEventReferenceId { get; private set; }

    private ScanAttempt() { }

    private ScanAttempt(
        ScanAttemptId id,
        PublishedEventReferenceId publishedEventReferenceId,
        SubmittedTicketCode submittedTicketCode,
        TicketCode? normalizedTicketCode,
        ScanOutcome outcome,
        ScanRejectionCategory rejectionCategory,
        DateTimeOffset scannedAt,
        TicketId? ticketId
        ) : base(id)
    {
        if (publishedEventReferenceId.IsEmpty)
            throw new ArgumentException("Published Event Reference ID cannot be empty.", nameof(publishedEventReferenceId));

        if (id.IsEmpty)
            throw new ArgumentException("Scan attempt ID cannot be empty.", nameof(id));

        if (normalizedTicketCode.HasValue && normalizedTicketCode.Value.IsEmpty)
            throw new ArgumentException("Normalized ticket code cannot be empty when provided.", nameof(normalizedTicketCode));

        if (scannedAt == default)
            throw new ArgumentException("Scanned timestamp cannot be the default value.", nameof(scannedAt));

        if (ticketId.HasValue && ticketId.Value.IsEmpty)
            throw new ArgumentException("Ticket ID cannot be empty when provided.", nameof(ticketId));

        if (!Enum.IsDefined(outcome))
            throw new ArgumentException("Invalid scan outcome.", nameof(outcome));

        if (!Enum.IsDefined(rejectionCategory))
            throw new ArgumentException("Invalid scan rejection category.", nameof(rejectionCategory));

        ArgumentNullException.ThrowIfNull(submittedTicketCode);

        if (outcome == ScanOutcome.Rejected && rejectionCategory == ScanRejectionCategory.None)
            throw new ArgumentException("A rejection reason category must be provided for rejected scan attempts.", nameof(rejectionCategory));

        if (outcome == ScanOutcome.Accepted && rejectionCategory != ScanRejectionCategory.None)
            throw new ArgumentException("A rejection reason category must not be provided for accepted scan attempts.", nameof(rejectionCategory));

        if (outcome == ScanOutcome.Accepted && !ticketId.HasValue)
            throw new ArgumentException("Accepted scan attempts must include a ticket ID.", nameof(ticketId));

        if (outcome == ScanOutcome.Accepted && !normalizedTicketCode.HasValue)
            throw new ArgumentException("Accepted scan attempts must include a normalized ticket code.", nameof(normalizedTicketCode));

        SubmittedTicketCode = submittedTicketCode;
        NormalizedTicketCode = normalizedTicketCode;
        Outcome = outcome;
        RejectionCategory = rejectionCategory;
        ScannedAt = scannedAt;
        TicketId = ticketId;
        PublishedEventReferenceId = publishedEventReferenceId;
    }

    public static ScanAttempt MalformedTicketCode(PublishedEventReferenceId publishedEventReferenceId, SubmittedTicketCode submittedTicketCode, DateTimeOffset scannedAt) => new(
        id: ScanAttemptId.Create(),
        publishedEventReferenceId: publishedEventReferenceId,
        submittedTicketCode: submittedTicketCode,
        normalizedTicketCode: null,
        outcome: ScanOutcome.Rejected,
        rejectionCategory: ScanRejectionCategory.MalformedTicketCode,
        scannedAt: scannedAt,
        ticketId: null
    );

    public static ScanAttempt TicketNotFound(
        SubmittedTicketCode submittedTicketCode,
        TicketCode normalizedTicketCode,
        PublishedEventReferenceId publishedEventReferenceId,
        DateTimeOffset scannedAt) => new(
            id: ScanAttemptId.Create(),
            publishedEventReferenceId: publishedEventReferenceId,
            submittedTicketCode: submittedTicketCode,
            normalizedTicketCode: normalizedTicketCode,
            outcome: ScanOutcome.Rejected,
            rejectionCategory: ScanRejectionCategory.TicketNotFound,
            scannedAt: scannedAt,
            ticketId: null);

    public static ScanAttempt IncorrectEvent(
        SubmittedTicketCode submittedTicketCode,
        TicketCode normalizedTicketCode,
        TicketId ticketId,
        PublishedEventReferenceId publishedEventReferenceId,
        DateTimeOffset scannedAt) => new(
            id: ScanAttemptId.Create(),
            publishedEventReferenceId: publishedEventReferenceId,
            submittedTicketCode: submittedTicketCode,
            normalizedTicketCode: normalizedTicketCode,
            outcome: ScanOutcome.Rejected,
            rejectionCategory: ScanRejectionCategory.IncorrectEvent,
            scannedAt: scannedAt,
            ticketId: ticketId);

    public static ScanAttempt CanceledTicket(
        SubmittedTicketCode submittedTicketCode,
        TicketCode normalizedTicketCode,
        TicketId ticketId,
        PublishedEventReferenceId publishedEventReferenceId,
        DateTimeOffset scannedAt) => new(
            id: ScanAttemptId.Create(),
            publishedEventReferenceId: publishedEventReferenceId,
            submittedTicketCode: submittedTicketCode,
            normalizedTicketCode: normalizedTicketCode,
            outcome: ScanOutcome.Rejected,
            rejectionCategory: ScanRejectionCategory.TicketCanceled,
            scannedAt: scannedAt,
            ticketId: ticketId);

    public static ScanAttempt DuplicateScan(
        SubmittedTicketCode submittedTicketCode,
        TicketCode normalizedTicketCode,
        TicketId ticketId,
        PublishedEventReferenceId publishedEventReferenceId,
        DateTimeOffset scannedAt) => new(
            id: ScanAttemptId.Create(),
            publishedEventReferenceId: publishedEventReferenceId,
            submittedTicketCode: submittedTicketCode,
            normalizedTicketCode: normalizedTicketCode,
            outcome: ScanOutcome.Rejected,
            rejectionCategory: ScanRejectionCategory.DuplicateScan,
            scannedAt: scannedAt,
            ticketId: ticketId);

    public static ScanAttempt ValidationUnavailable(
        SubmittedTicketCode submittedTicketCode,
        TicketCode? normalizedTicketCode,
        PublishedEventReferenceId publishedEventReferenceId,
        DateTimeOffset scannedAt,
        TicketId? ticketId = null) => new(
            id: ScanAttemptId.Create(),
            publishedEventReferenceId: publishedEventReferenceId,
            submittedTicketCode: submittedTicketCode,
            normalizedTicketCode: normalizedTicketCode,
            outcome: ScanOutcome.Rejected,
            rejectionCategory: ScanRejectionCategory.ValidationUnavailable,
            scannedAt: scannedAt,
            ticketId: ticketId);

    public static ScanAttempt UnexpectedError(
        SubmittedTicketCode submittedTicketCode,
        TicketCode? normalizedTicketCode,
        PublishedEventReferenceId publishedEventReferenceId,
        DateTimeOffset scannedAt,
        TicketId? ticketId = null) => new(
            id: ScanAttemptId.Create(),
            publishedEventReferenceId: publishedEventReferenceId,
            submittedTicketCode: submittedTicketCode,
            normalizedTicketCode: normalizedTicketCode,
            outcome: ScanOutcome.Rejected,
            rejectionCategory: ScanRejectionCategory.UnexpectedError,
            scannedAt: scannedAt,
            ticketId: ticketId);

    public static ScanAttempt Accepted(
        SubmittedTicketCode submittedTicketCode,
        TicketCode normalizedTicketCode,
        TicketId ticketId,
        PublishedEventReferenceId publishedEventReferenceId,
        DateTimeOffset scannedAt) => new(
            id: ScanAttemptId.Create(),
            publishedEventReferenceId: publishedEventReferenceId,
            submittedTicketCode: submittedTicketCode,
            normalizedTicketCode: normalizedTicketCode,
            outcome: ScanOutcome.Accepted,
            rejectionCategory: ScanRejectionCategory.None,
            scannedAt: scannedAt,
            ticketId: ticketId);

}

public readonly record struct ScanAttemptId(Guid Value)
{
    public static ScanAttemptId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(ScanAttemptId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}

public enum ScanOutcome
{
    Accepted = 1,
    Rejected = 2
}

public enum ScanRejectionCategory
{
    None = 0,

    MalformedTicketCode = 1,
    TicketNotFound = 2,
    TicketCanceled = 3,
    IncorrectEvent = 4,
    DuplicateScan = 5,

    ValidationUnavailable = 98,
    UnexpectedError = 99,
}

public record SubmittedTicketCode
{
    public const int MaxLength = 128;

    public string Value { get; }

    public SubmittedTicketCode(string value)
    {
        value = value?.Trim() ?? string.Empty;

        value = value.Length > MaxLength ? value[..MaxLength] : value;

        Value = value;
    }

    public static implicit operator string(SubmittedTicketCode code) => code.Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
}
