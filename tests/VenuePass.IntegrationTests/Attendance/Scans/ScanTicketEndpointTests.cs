using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.IntegrationTests.Ticketing.Fixtures;
using VenuePass.Modules.Attendance.Contracts;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.ScanAttempts;
using VenuePass.Modules.Attendance.Infrastructure;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Attendance.Scans;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class ScanTicketEndpointTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;
    private readonly HttpClient _operatorClient;
    private readonly HttpClient _unauthenticatedClient;
    private readonly HttpClient _customerClient;

    public ScanTicketEndpointTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
        _operatorClient = CreateAttendanceOperatorClient(fixture);
        _unauthenticatedClient = fixture.Client;
        _customerClient = fixture.CreateAuthenticatedCustomerClient();
    }

    [Fact]
    public async Task ScanTicket_WhenValidIssuedTicket_Returns201_AndPersistsAttendanceOutcomes()
    {
        IssuedTicketSeed issuedTicket = await CreateIssuedTicketAsync();

        var response = await _operatorClient.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(issuedTicket.TicketCode, issuedTicket.PublishedEventReferenceId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ScanTicketResponse>();
        Assert.NotNull(body);
        Assert.Equal(issuedTicket.TicketId, body!.TicketId);
        Assert.Equal(issuedTicket.TicketCode, body.TicketCode);
        Assert.Equal(issuedTicket.PublishedEventReferenceId, body.PublishedEventReferenceId);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        var attendanceRecord = await attendanceDb.AttendanceRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TicketId == new TicketId(issuedTicket.TicketId));

        Assert.NotNull(attendanceRecord);

        var acceptedAttempts = await attendanceDb.ScanAttempts
            .AsNoTracking()
            .Where(x => x.TicketId == new TicketId(issuedTicket.TicketId))
            .ToListAsync();

        Assert.Single(acceptedAttempts);
        Assert.Equal(ScanOutcome.Accepted, acceptedAttempts[0].Outcome);

        var checkedInMessages = await LoadCheckedInMessagesAsync(attendanceDb, issuedTicket.TicketId);
        Assert.Single(checkedInMessages);

        var checkedInMessage = checkedInMessages[0];
        Assert.Equal(issuedTicket.TicketId, checkedInMessage.TicketId);
        Assert.Equal(issuedTicket.TicketCode, checkedInMessage.TicketCode);
        Assert.Equal(issuedTicket.PublishedEventReferenceId, checkedInMessage.PublishedEventId);
    }

    [Fact]
    public async Task ScanTicket_WhenCallerIsNotAttendanceOperator_Returns403()
    {
        IssuedTicketSeed issuedTicket = await CreateIssuedTicketAsync();

        var response = await _managerClient.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(issuedTicket.TicketCode, issuedTicket.PublishedEventReferenceId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ScanTicket_WhenPublishedEventReferenceIdIsEmpty_Returns400()
    {
        IssuedTicketSeed issuedTicket = await CreateIssuedTicketAsync();

        var response = await _operatorClient.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(issuedTicket.TicketCode, Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ScanTicket_WhenUnauthenticated_Returns401()
    {
        IssuedTicketSeed issuedTicket = await CreateIssuedTicketAsync();

        var response = await _unauthenticatedClient.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(issuedTicket.TicketCode, issuedTicket.PublishedEventReferenceId));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ScanTicket_WhenTicketScannedTwice_ReturnsConflict_AndPersistsDuplicateRejectedAttempt()
    {
        IssuedTicketSeed issuedTicket = await CreateIssuedTicketAsync();

        HttpResponseMessage firstResponse = await _operatorClient.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(issuedTicket.TicketCode, issuedTicket.PublishedEventReferenceId));

        HttpResponseMessage secondResponse = await _operatorClient.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(issuedTicket.TicketCode, issuedTicket.PublishedEventReferenceId));

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        ProblemDetails? duplicateProblem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(duplicateProblem);
        Assert.Equal("Attendance.ScanTicket.TicketAlreadyScanned", GetProblemCode(duplicateProblem!));

        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        AttendanceDbContext attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        int attendanceCount = await attendanceDb.AttendanceRecords
            .AsNoTracking()
            .CountAsync(x => x.TicketId == new TicketId(issuedTicket.TicketId));

        List<ScanAttempt> scanAttempts = await attendanceDb.ScanAttempts
            .AsNoTracking()
            .Where(x => x.TicketId == new TicketId(issuedTicket.TicketId))
            .OrderBy(x => x.ScannedAt)
            .ToListAsync();

        List<TicketCheckedInIntegrationEvent> checkedInMessages = await LoadCheckedInMessagesAsync(attendanceDb, issuedTicket.TicketId);

        Assert.Equal(1, attendanceCount);
        Assert.Equal(2, scanAttempts.Count);
        Assert.Single(scanAttempts, x => x.Outcome == ScanOutcome.Accepted);
        Assert.Single(scanAttempts, x => x.Outcome == ScanOutcome.Rejected && x.RejectionCategory == ScanRejectionCategory.DuplicateScan);
        Assert.Single(checkedInMessages);
    }

    [Fact]
    public async Task ScanTicket_WhenConcurrentScansForSameTicket_OneSucceedsAndOneConflicts_WithSingleOutboxMessage()
    {
        IssuedTicketSeed issuedTicket = await CreateIssuedTicketAsync();

        HttpClient firstOperatorClient = CreateAttendanceOperatorClient(_fixture);
        HttpClient secondOperatorClient = CreateAttendanceOperatorClient(_fixture);

        Task<HttpResponseMessage> firstAttempt = firstOperatorClient.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(issuedTicket.TicketCode, issuedTicket.PublishedEventReferenceId));

        Task<HttpResponseMessage> secondAttempt = secondOperatorClient.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(issuedTicket.TicketCode, issuedTicket.PublishedEventReferenceId));

        HttpResponseMessage[] responses = await Task.WhenAll(firstAttempt, secondAttempt);

        int successCount = responses.Count(x => x.StatusCode == HttpStatusCode.Created);
        int conflictCount = responses.Count(x => x.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        HttpResponseMessage conflictResponse = responses.Single(x => x.StatusCode == HttpStatusCode.Conflict);
        ProblemDetails? conflictProblem = await conflictResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(conflictProblem);
        Assert.Equal("Attendance.ScanTicket.TicketAlreadyScanned", GetProblemCode(conflictProblem!));

        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        AttendanceDbContext attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        int attendanceCount = await attendanceDb.AttendanceRecords
            .AsNoTracking()
            .CountAsync(x => x.TicketId == new TicketId(issuedTicket.TicketId));

        List<ScanAttempt> scanAttempts = await attendanceDb.ScanAttempts
            .AsNoTracking()
            .Where(x => x.TicketId == new TicketId(issuedTicket.TicketId))
            .ToListAsync();

        List<TicketCheckedInIntegrationEvent> checkedInMessages = await LoadCheckedInMessagesAsync(attendanceDb, issuedTicket.TicketId);

        Assert.Equal(1, attendanceCount);
        Assert.Single(checkedInMessages);
        Assert.Single(scanAttempts, x => x.Outcome == ScanOutcome.Accepted);
        Assert.Single(scanAttempts, x => x.Outcome == ScanOutcome.Rejected && x.RejectionCategory == ScanRejectionCategory.DuplicateScan);
    }

    [Fact]
    public async Task ScanTicket_WhenSuccessful_PersistsAttendanceAndOutboxAtomically()
    {
        IssuedTicketSeed issuedTicket = await CreateIssuedTicketAsync();

        HttpResponseMessage response = await _operatorClient.PostAsJsonAsync(
            "/attendance/scans",
            new ScanTicketRequest(issuedTicket.TicketCode, issuedTicket.PublishedEventReferenceId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        AttendanceDbContext attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        int attendanceCount = await attendanceDb.AttendanceRecords
            .AsNoTracking()
            .CountAsync(x => x.TicketId == new TicketId(issuedTicket.TicketId));

        int acceptedAttemptCount = await attendanceDb.ScanAttempts
            .AsNoTracking()
            .CountAsync(x => x.TicketId == new TicketId(issuedTicket.TicketId) && x.Outcome == ScanOutcome.Accepted);

        List<TicketCheckedInIntegrationEvent> checkedInMessages = await LoadCheckedInMessagesAsync(attendanceDb, issuedTicket.TicketId);

        Assert.Equal(1, attendanceCount);
        Assert.Equal(1, acceptedAttemptCount);
        Assert.Single(checkedInMessages);
    }

    private async Task<IssuedTicketSeed> CreateIssuedTicketAsync()
    {
        await EnsureAttendanceSchemaAsync();

        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, _) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(
            _managerClient,
            offerId,
            [new PriceZoneItem("Zone A", 25m, seatIds, [])]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient,
            offerId,
            seatIds: [seatIds[0]]);

        Guid orderId = await TicketingSeedHelpers.CheckoutReservationAsync(_customerClient, reservationId);
        GetOrderSeedResponse order = await TicketingSeedHelpers.GetOrderAsync(_customerClient, orderId);

        var ticket = Assert.Single(order.Tickets);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var ticketingDb = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var persistedTicket = await ticketingDb.Tickets
            .AsNoTracking()
            .SingleAsync(x => x.Id == new VenuePass.Modules.Ticketing.Domain.Tickets.TicketId(ticket.TicketId));

        var ticketingReference = await ticketingDb.PublishedEventReferences
            .AsNoTracking()
            .SingleAsync(x => x.Id == persistedTicket.PublishedEventReferenceId);

        bool referenceExists = await attendanceDb.PublishedEventReferences
            .AsNoTracking()
            .AnyAsync(x => x.Id == new PublishedEventReferenceId(persistedTicket.PublishedEventReferenceId.Value));

        if (!referenceExists)
        {
            attendanceDb.PublishedEventReferences.Add(PublishedEventReference.Create(
                id: new PublishedEventReferenceId(ticketingReference.Id.Value),
                eventId: ticketingReference.EventId,
                manifestId: ticketingReference.ManifestId,
                syncedAt: ticketingReference.SyncedAt));

            await attendanceDb.SaveChangesAsync();
        }

        return new IssuedTicketSeed(
            TicketId: ticket.TicketId,
            TicketCode: ticket.Code,
            PublishedEventReferenceId: persistedTicket.PublishedEventReferenceId.Value);
    }

    private async Task EnsureAttendanceSchemaAsync()
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        await attendanceDb.Database.MigrateAsync();
    }

    private static HttpClient CreateAttendanceOperatorClient(EventsIntegrationTestFixture fixture)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "AttendanceOperator");
        return client;
    }

    private static async Task<List<TicketCheckedInIntegrationEvent>> LoadCheckedInMessagesAsync(
        AttendanceDbContext db,
        Guid ticketId)
    {
        var candidates = await db.OutboxMessages
            .AsNoTracking()
            .Where(x => x.Type == typeof(TicketCheckedInIntegrationEvent).AssemblyQualifiedName)
            .OrderByDescending(x => x.OccurredOn)
            .ToListAsync();

        var matching = new List<TicketCheckedInIntegrationEvent>();

        foreach (var candidate in candidates)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<TicketCheckedInIntegrationEvent>(candidate.Payload);
                if (payload is not null && payload.TicketId == ticketId)
                {
                    matching.Add(payload);
                }
            }
            catch (JsonException)
            {
                // Ignore unrelated malformed payloads.
            }
        }

        return matching;
    }

    private static string? GetProblemCode(ProblemDetails details)
    {
        if (!details.Extensions.TryGetValue("code", out object? value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string text => text,
            JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString(),
            _ => value.ToString()
        };
    }

    private sealed record ScanTicketRequest(string TicketCode, Guid PublishedEventReferenceId);

    private sealed record ScanTicketResponse(
        Guid AttendanceRecordId,
        Guid TicketId,
        string TicketCode,
        Guid PublishedEventReferenceId,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        Guid OrderId,
        Guid OrderItemId,
        DateTimeOffset CheckedInAt);

    private sealed record IssuedTicketSeed(Guid TicketId, string TicketCode, Guid PublishedEventReferenceId);
}
