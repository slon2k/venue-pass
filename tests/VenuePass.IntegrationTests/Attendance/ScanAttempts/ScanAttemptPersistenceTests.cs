using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.ScanAttempts;
using VenuePass.Modules.Attendance.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Attendance.ScanAttempts;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class ScanAttemptPersistenceTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public ScanAttemptPersistenceTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Save_AcceptedAndRejectedScanAttempts_BothPersist()
    {
        string isolatedConnectionString = AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
            _fixture.ConnectionString,
            "scan_attempt_persistence");

        ServiceProvider services = AttendanceIntegrationTestHelper.CreateServicesWithEnsureCreated(isolatedConnectionString);
        PublishedEventReferenceId publishedEventReferenceId = await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        ScanAttempt accepted = ScanAttempt.Accepted(
            submittedTicketCode: new SubmittedTicketCode("0000000000000201"),
            normalizedTicketCode: new TicketCode("0000000000000201"),
            ticketId: new TicketId(Guid.CreateVersion7()),
            publishedEventReferenceId: publishedEventReferenceId,
            scannedAt: DateTimeOffset.UtcNow);

        ScanAttempt rejected = ScanAttempt.UnknownTicket(
            submittedTicketCode: new SubmittedTicketCode("INVALID-CODE"),
            normalizedTicketCode: new TicketCode("0000000000000202"),
            publishedEventReferenceId: publishedEventReferenceId,
            scannedAt: DateTimeOffset.UtcNow.AddSeconds(1));

        await SaveScanAttemptAsync(services, accepted);
        await SaveScanAttemptAsync(services, rejected);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        int acceptedCount = await db.ScanAttempts.CountAsync(x => x.Outcome == ScanOutcome.Accepted);
        int rejectedCount = await db.ScanAttempts.CountAsync(x => x.Outcome == ScanOutcome.Rejected);

        Assert.Equal(1, acceptedCount);
        Assert.Equal(1, rejectedCount);
    }

    [Fact]
    public async Task Save_RejectedScanAttempt_PersistsRejectionCategory()
    {
        string isolatedConnectionString = AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
            _fixture.ConnectionString,
            "scan_attempt_persistence");

        ServiceProvider services = AttendanceIntegrationTestHelper.CreateServicesWithEnsureCreated(isolatedConnectionString);
        PublishedEventReferenceId publishedEventReferenceId = await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        ScanAttempt rejected = ScanAttempt.DuplicateScan(
            submittedTicketCode: new SubmittedTicketCode("0000000000000203"),
            normalizedTicketCode: new TicketCode("0000000000000203"),
            ticketId: new TicketId(Guid.CreateVersion7()),
            publishedEventReferenceId: publishedEventReferenceId,
            scannedAt: DateTimeOffset.UtcNow);

        await SaveScanAttemptAsync(services, rejected);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        ScanAttempt? saved = await db.ScanAttempts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == rejected.Id);

        Assert.NotNull(saved);
        Assert.Equal(ScanOutcome.Rejected, saved!.Outcome);
        Assert.Equal(ScanRejectionCategory.DuplicateScan, saved.RejectionCategory);
    }

    private static async Task SaveScanAttemptAsync(IServiceProvider services, ScanAttempt scanAttempt)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        db.ScanAttempts.Add(scanAttempt);
        await db.SaveChangesAsync();
    }
}
