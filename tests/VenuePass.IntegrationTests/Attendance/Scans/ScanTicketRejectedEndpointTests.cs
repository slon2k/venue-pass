using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Attendance.Contracts;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.ScanAttempts;
using VenuePass.Modules.Attendance.Infrastructure;
using VenuePass.Modules.Attendance.Features.ScanTicket;
using VenuePass.Modules.Ticketing.Contracts;

using Xunit;

namespace VenuePass.IntegrationTests.Attendance.Scans;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class ScanTicketRejectedEndpointTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public ScanTicketRejectedEndpointTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ScanTicket_WhenMalformedTicketCode_Returns400AndPersistsRejectedAttempt()
    {
        Guid publishedEventReferenceId = Guid.CreateVersion7();
        await using EventsApiFactory factory = await CreateFactoryAsync(
            publishedEventReferenceId,
            new StubTicketingModuleContract((_, _, _) => throw new InvalidOperationException("Validation should not be called for malformed ticket codes.")));

        HttpClient client = CreateAttendanceOperatorClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest("not-a-ticket-code", publishedEventReferenceId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertRejectedAttemptAsync(
            factory,
            publishedEventReferenceId,
            "not-a-ticket-code",
            ScanRejectionCategory.MalformedTicketCode);
    }

    [Fact]
    public async Task ScanTicket_WhenUnknownTicket_Returns404AndPersistsRejectedAttempt()
    {
        Guid publishedEventReferenceId = Guid.CreateVersion7();
        await using EventsApiFactory factory = await CreateFactoryAsync(
            publishedEventReferenceId,
            new StubTicketingModuleContract((_, _, _) => Task.FromResult(TicketValidationResultDto.TicketNotFound())));

        HttpClient client = CreateAttendanceOperatorClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest("0000000000000001", publishedEventReferenceId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await AssertRejectedAttemptAsync(
            factory,
            publishedEventReferenceId,
            "0000000000000001",
            ScanRejectionCategory.UnknownTicket);
    }

    [Fact]
    public async Task ScanTicket_WhenInvalidTicket_Returns400AndPersistsRejectedAttempt()
    {
        Guid publishedEventReferenceId = Guid.CreateVersion7();
        Guid ticketId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        Guid orderItemId = Guid.CreateVersion7();
        var ticket = TicketExportDto.CreateForSeat(
            ticketId: ticketId,
            publishedEventReferenceId: publishedEventReferenceId,
            orderId: orderId,
            orderItemId: orderItemId,
            code: "0000000000000002",
            status: TicketValidationStatus.Issued,
            inventorySeatId: Guid.CreateVersion7(),
            issuedAt: DateTimeOffset.UtcNow);

        await using EventsApiFactory factory = await CreateFactoryAsync(
            publishedEventReferenceId,
            new StubTicketingModuleContract((_, _, _) => Task.FromResult(TicketValidationResultDto.InvalidTicket(ticket))));

        HttpClient client = CreateAttendanceOperatorClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(ticket.TicketCode, publishedEventReferenceId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertRejectedAttemptAsync(
            factory,
            publishedEventReferenceId,
            ticket.TicketCode,
            ScanRejectionCategory.InvalidTicket);
    }

    [Fact]
    public async Task ScanTicket_WhenCanceledTicket_Returns409AndPersistsRejectedAttempt()
    {
        Guid publishedEventReferenceId = Guid.CreateVersion7();
        var ticket = TicketExportDto.CreateForSeat(
            ticketId: Guid.CreateVersion7(),
            publishedEventReferenceId: publishedEventReferenceId,
            orderId: Guid.CreateVersion7(),
            orderItemId: Guid.CreateVersion7(),
            code: "0000000000000003",
            status: TicketValidationStatus.Canceled,
            inventorySeatId: Guid.CreateVersion7(),
            issuedAt: DateTimeOffset.UtcNow);

        await using EventsApiFactory factory = await CreateFactoryAsync(
            publishedEventReferenceId,
            new StubTicketingModuleContract((_, _, _) => Task.FromResult(TicketValidationResultDto.CreateForPublishedEventReference(ticket, publishedEventReferenceId))));

        HttpClient client = CreateAttendanceOperatorClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(ticket.TicketCode, publishedEventReferenceId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await AssertRejectedAttemptAsync(
            factory,
            publishedEventReferenceId,
            ticket.TicketCode,
            ScanRejectionCategory.TicketCanceled);
    }

    [Fact]
    public async Task ScanTicket_WhenValidationTimesOut_Returns503AndPersistsValidationUnavailableAttempt()
    {
        Guid publishedEventReferenceId = Guid.CreateVersion7();
        await using EventsApiFactory factory = await CreateFactoryAsync(
            publishedEventReferenceId,
            new StubTicketingModuleContract((_, _, _) => throw new TimeoutException("simulated timeout")));

        HttpClient client = CreateAttendanceOperatorClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest("0000000000000004", publishedEventReferenceId));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        await AssertRejectedAttemptAsync(
            factory,
            publishedEventReferenceId,
            "0000000000000004",
            ScanRejectionCategory.ValidationUnavailable);
    }

    [Fact]
    public async Task ScanTicket_WhenValidationIsUnavailable_Returns503AndPersistsValidationUnavailableAttempt()
    {
        Guid publishedEventReferenceId = Guid.CreateVersion7();
        await using EventsApiFactory factory = await CreateFactoryAsync(
            publishedEventReferenceId,
            new StubTicketingModuleContract((_, _, _) => throw new InvalidOperationException("simulated unavailable dependency")));

        HttpClient client = CreateAttendanceOperatorClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest("0000000000000005", publishedEventReferenceId));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        await AssertRejectedAttemptAsync(
            factory,
            publishedEventReferenceId,
            "0000000000000005",
            ScanRejectionCategory.ValidationUnavailable);
    }

    private async Task<EventsApiFactory> CreateFactoryAsync(
        Guid publishedEventReferenceId,
        ITicketingModuleContract ticketingContract)
    {
        EventsApiFactory factory = _fixture.CreateFactory(configureTestServices: services =>
        {
            services.AddSingleton(ticketingContract);
        });

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
            await db.Database.MigrateAsync();

            bool referenceExists = await db.PublishedEventReferences
                .AsNoTracking()
                .AnyAsync(x => x.Id == new PublishedEventReferenceId(publishedEventReferenceId));

            if (!referenceExists)
            {
                db.PublishedEventReferences.Add(PublishedEventReference.Create(
                    id: new PublishedEventReferenceId(publishedEventReferenceId),
                    eventId: Guid.CreateVersion7(),
                    manifestId: Guid.CreateVersion7(),
                    syncedAt: DateTimeOffset.UtcNow));

                await db.SaveChangesAsync();
            }
        }

        return factory;
    }

    private static HttpClient CreateAttendanceOperatorClient(EventsApiFactory factory)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "AttendanceOperator");
        return client;
    }

    private static async Task AssertRejectedAttemptAsync(
        EventsApiFactory factory,
        Guid publishedEventReferenceId,
        string submittedTicketCode,
        ScanRejectionCategory expectedRejectionCategory)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        ScanAttempt attempt = await db.ScanAttempts
            .AsNoTracking()
            .SingleAsync(x =>
                x.PublishedEventReferenceId == new PublishedEventReferenceId(publishedEventReferenceId) &&
                x.SubmittedTicketCode == new SubmittedTicketCode(submittedTicketCode));

        Assert.Equal(ScanOutcome.Rejected, attempt.Outcome);
        Assert.Equal(expectedRejectionCategory, attempt.RejectionCategory);

        int attendanceCount = await db.AttendanceRecords.CountAsync();
        int checkedInOutboxCount = await db.OutboxMessages.CountAsync(x => x.Type == typeof(TicketCheckedInIntegrationEvent).AssemblyQualifiedName);

        Assert.Equal(0, attendanceCount);
        Assert.Equal(0, checkedInOutboxCount);
    }

    private sealed record ScanTicketRequest(string TicketCode, Guid PublishedEventReferenceId);

    private sealed class StubTicketingModuleContract(
        Func<string, Guid, CancellationToken, Task<TicketValidationResultDto>> validateTicket)
        : ITicketingModuleContract
    {
        public Task<TicketValidationResultDto> ValidateTicketForPublishedEventReferenceAsync(
            string ticketCode,
            Guid publishedEventReferenceId,
            CancellationToken cancellationToken = default)
            => validateTicket(ticketCode, publishedEventReferenceId, cancellationToken);
    }
}