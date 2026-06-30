using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Attendance.Domain.AttendanceRecords;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Attendance.AttendanceRecords;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class AttendanceRecordPersistenceTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public AttendanceRecordPersistenceTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Save_DuplicateAttendanceRecordByTicketId_IsRejectedByPersistence()
    {
        string isolatedConnectionString = AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
            _fixture.ConnectionString,
            "attendance_record_persistence");

        ServiceProvider services = AttendanceIntegrationTestHelper.CreateServicesWithEnsureCreated(isolatedConnectionString);
        PublishedEventReferenceId publishedEventReferenceId = await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        var duplicateTicketId = new TicketId(Guid.CreateVersion7());

        AttendanceRecord first = AttendanceRecord.CreateForSeat(
            ticketId: duplicateTicketId,
            ticketCode: new TicketCode("0000000000000101"),
            publishedEventId: publishedEventReferenceId,
            inventorySeatId: new InventorySeatId(Guid.CreateVersion7()),
            checkedInAt: DateTimeOffset.UtcNow,
            orderId: new OrderId(Guid.CreateVersion7()),
            orderItemId: new OrderItemId(Guid.CreateVersion7()));

        AttendanceRecord second = AttendanceRecord.CreateForSeat(
            ticketId: duplicateTicketId,
            ticketCode: new TicketCode("0000000000000102"),
            publishedEventId: publishedEventReferenceId,
            inventorySeatId: new InventorySeatId(Guid.CreateVersion7()),
            checkedInAt: DateTimeOffset.UtcNow.AddSeconds(1),
            orderId: new OrderId(Guid.CreateVersion7()),
            orderItemId: new OrderItemId(Guid.CreateVersion7()));

        await SaveAttendanceRecordAsync(services, first);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await SaveAttendanceRecordAsync(services, second));
    }

    private static async Task SaveAttendanceRecordAsync(IServiceProvider services, AttendanceRecord attendanceRecord)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        db.AttendanceRecords.Add(attendanceRecord);
        await db.SaveChangesAsync();
    }
}
