using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.TicketProjections;
using VenuePass.Modules.Attendance.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Attendance.TicketProjections;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class TicketProjectionPersistenceTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public TicketProjectionPersistenceTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Save_DuplicateTicketId_IsRejectedByPersistence()
    {
        string isolatedConnectionString = AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
            _fixture.ConnectionString,
            "attendance_projection_test");

        ServiceProvider services = AttendanceIntegrationTestHelper.CreateServicesWithEnsureCreated(isolatedConnectionString);
        PublishedEventReferenceId publishedEventReferenceId = await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        var duplicateTicketId = new TicketId(Guid.CreateVersion7());

        var first = CreateProjection(
            ticketId: duplicateTicketId,
            ticketCode: new TicketCode("0000000000000001"),
            publishedEventReferenceId: publishedEventReferenceId);

        var second = CreateProjection(
            ticketId: duplicateTicketId,
            ticketCode: new TicketCode("0000000000000002"),
            publishedEventReferenceId: publishedEventReferenceId);

        await SaveProjectionAsync(services, first);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await SaveProjectionAsync(services, second));
    }

    [Fact]
    public async Task Save_DuplicateTicketCode_IsRejectedByPersistence()
    {
        string isolatedConnectionString = AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
            _fixture.ConnectionString,
            "attendance_projection_test");

        ServiceProvider services = AttendanceIntegrationTestHelper.CreateServicesWithEnsureCreated(isolatedConnectionString);
        PublishedEventReferenceId publishedEventReferenceId = await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        var duplicateCode = new TicketCode("0000000000000003");

        var first = CreateProjection(
            ticketId: new TicketId(Guid.CreateVersion7()),
            ticketCode: duplicateCode,
            publishedEventReferenceId: publishedEventReferenceId);

        var second = CreateProjection(
            ticketId: new TicketId(Guid.CreateVersion7()),
            ticketCode: duplicateCode,
            publishedEventReferenceId: publishedEventReferenceId);

        await SaveProjectionAsync(services, first);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await SaveProjectionAsync(services, second));
    }

    [Fact]
    public async Task Lookup_ByTicketIdAndTicketCode_ReturnsStoredProjection()
    {
        string isolatedConnectionString = AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
            _fixture.ConnectionString,
            "attendance_projection_test");

        ServiceProvider services = AttendanceIntegrationTestHelper.CreateServicesWithEnsureCreated(isolatedConnectionString);
        PublishedEventReferenceId publishedEventReferenceId = await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        var projection = CreateProjection(
            ticketId: new TicketId(Guid.CreateVersion7()),
            ticketCode: new TicketCode("0000000000000004"),
            publishedEventReferenceId: publishedEventReferenceId);

        await SaveProjectionAsync(services, projection);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        TicketProjection? byTicketId = await db.TicketProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == projection.Id);

        TicketProjection? byTicketCode = await db.TicketProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TicketCode == projection.TicketCode);

        Assert.NotNull(byTicketId);
        Assert.NotNull(byTicketCode);
        Assert.Equal(projection.Id, byTicketId!.Id);
        Assert.Equal(projection.Id, byTicketCode!.Id);
    }

    private static async Task SaveProjectionAsync(IServiceProvider services, TicketProjection projection)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        db.TicketProjections.Add(projection);
        await db.SaveChangesAsync();
    }

    private static TicketProjection CreateProjection(
        TicketId ticketId,
        TicketCode ticketCode,
        PublishedEventReferenceId publishedEventReferenceId)
        => TicketProjection.Create(
            id: ticketId,
            ticketCode: ticketCode,
            publishedEventReferenceId: publishedEventReferenceId,
            orderId: new OrderId(Guid.CreateVersion7()),
            orderItemId: new OrderItemId(Guid.CreateVersion7()),
            inventoryId: new InventoryId(Guid.CreateVersion7()),
            inventorySeatId: new InventorySeatId(Guid.CreateVersion7()),
            generalAdmissionPoolId: null,
            issuedAt: DateTimeOffset.UtcNow);
}
