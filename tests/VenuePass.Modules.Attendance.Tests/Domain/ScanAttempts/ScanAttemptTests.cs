using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.ScanAttempts;

using Xunit;

namespace VenuePass.Modules.Attendance.Tests.Domain.ScanAttempts;

public sealed class ScanAttemptTests
{
    [Fact]
    public void Accepted_WithValidInput_CreatesAcceptedAttempt()
    {
        var scannedAt = new DateTimeOffset(2026, 6, 27, 15, 0, 0, TimeSpan.Zero);

        var attempt = ScanAttempt.Accepted(
            submittedTicketCode: "01AR-Z3ND-EKTS-V4RR",
            normalizedTicketCode: new TicketCode("01ARZ3NDEKTSV4RR"),
            ticketId: new TicketId(Guid.CreateVersion7()),
            publishedEventReferenceId: new PublishedEventReferenceId(Guid.CreateVersion7()),
            scannedAt: scannedAt);

        Assert.Equal(ScanOutcome.Accepted, attempt.Outcome);
        Assert.Equal(ScanRejectionCategory.None, attempt.RejectionCategory);
        Assert.Equal(scannedAt, attempt.ScannedAt);
        Assert.NotNull(attempt.NormalizedTicketCode);
        Assert.True(attempt.TicketId.HasValue);
    }

    [Fact]
    public void MalformedTicketCode_WithValidInput_CreatesRejectedAttempt()
    {
        var scannedAt = new DateTimeOffset(2026, 6, 27, 15, 5, 0, TimeSpan.Zero);

        var attempt = ScanAttempt.MalformedTicketCode(
            publishedEventReferenceId: new PublishedEventReferenceId(Guid.CreateVersion7()),
            submittedTicketCode: "BAD-CODE",
            scannedAt: scannedAt);

        Assert.Equal(ScanOutcome.Rejected, attempt.Outcome);
        Assert.Equal(ScanRejectionCategory.MalformedTicketCode, attempt.RejectionCategory);
        Assert.Equal(scannedAt, attempt.ScannedAt);
        Assert.Null(attempt.NormalizedTicketCode);
        Assert.False(attempt.TicketId.HasValue);
    }

    [Fact]
    public void ValidationUnavailable_WithNormalizedCodeAndTicket_CreatesRejectedAttempt()
    {
        var scannedAt = new DateTimeOffset(2026, 6, 27, 15, 10, 0, TimeSpan.Zero);
        var normalized = new TicketCode("01ARZ3NDEKTSV4RR");
        var ticketId = new TicketId(Guid.CreateVersion7());

        var attempt = ScanAttempt.ValidationUnavailable(
            submittedTicketCode: "01AR-Z3ND-EKTS-V4RR",
            normalizedTicketCode: normalized,
            publishedEventReferenceId: new PublishedEventReferenceId(Guid.CreateVersion7()),
            scannedAt: scannedAt,
            ticketId: ticketId);

        Assert.Equal(ScanOutcome.Rejected, attempt.Outcome);
        Assert.Equal(ScanRejectionCategory.ValidationUnavailable, attempt.RejectionCategory);
        Assert.Equal(normalized, attempt.NormalizedTicketCode);
        Assert.Equal(ticketId, attempt.TicketId);
    }

    [Fact]
    public void Accepted_WithEmptyTicketId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ScanAttempt.Accepted(
                submittedTicketCode: "01AR-Z3ND-EKTS-V4RR",
                normalizedTicketCode: new TicketCode("01ARZ3NDEKTSV4RR"),
                ticketId: new TicketId(Guid.Empty),
                publishedEventReferenceId: new PublishedEventReferenceId(Guid.CreateVersion7()),
                scannedAt: DateTimeOffset.UtcNow));

        Assert.Contains("Ticket ID cannot be empty", exception.Message);
    }

    [Fact]
    public void TicketNotFound_WithEmptyPublishedEventReference_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ScanAttempt.TicketNotFound(
                submittedTicketCode: "01AR-Z3ND-EKTS-V4RR",
                normalizedTicketCode: new TicketCode("01ARZ3NDEKTSV4RR"),
                publishedEventReferenceId: new PublishedEventReferenceId(Guid.Empty),
                scannedAt: DateTimeOffset.UtcNow));

        Assert.Contains("Published Event Reference ID cannot be empty", exception.Message);
    }

    [Fact]
    public void MalformedTicketCode_WithDefaultScannedAt_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ScanAttempt.MalformedTicketCode(
                publishedEventReferenceId: new PublishedEventReferenceId(Guid.CreateVersion7()),
                submittedTicketCode: "BAD-CODE",
                scannedAt: default));

        Assert.Contains("Scanned timestamp cannot be the default value", exception.Message);
    }
}