using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Attendance.Domain;
using VenuePass.Modules.Attendance.Domain.AttendanceRecords;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;

using Xunit;

namespace VenuePass.Modules.Attendance.Tests.Domain.AttendanceRecords;

public sealed class AttendanceRecordTests
{
    [Fact]
    public void CreateForSeat_WithValidInput_CreatesSeatAttendanceRecord()
    {
        var checkedInAt = new DateTimeOffset(2026, 6, 27, 14, 0, 0, TimeSpan.Zero);

        var record = AttendanceRecord.CreateForSeat(
            ticketId: new TicketId(Guid.CreateVersion7()),
            ticketCode: new TicketCode("01ARZ3NDEKTSV4RR"),
            publishedEventId: new PublishedEventReferenceId(Guid.CreateVersion7()),
            inventorySeatId: new InventorySeatId(Guid.CreateVersion7()),
            checkedInAt: checkedInAt,
            orderId: new OrderId(Guid.CreateVersion7()),
            orderItemId: new OrderItemId(Guid.CreateVersion7()));

        Assert.True(record.Id != default);
        Assert.Equal(checkedInAt, record.CheckedInAt);
        Assert.True(record.InventorySeatId.HasValue);
        Assert.False(record.GeneralAdmissionPoolId.HasValue);
    }

    [Fact]
    public void CreateForGeneralAdmission_WithValidInput_CreatesGaAttendanceRecord()
    {
        var checkedInAt = new DateTimeOffset(2026, 6, 27, 14, 5, 0, TimeSpan.Zero);

        var record = AttendanceRecord.CreateForGeneralAdmission(
            ticketId: new TicketId(Guid.CreateVersion7()),
            ticketCode: new TicketCode("01ARZ3NDEKTSV4RR"),
            publishedEventId: new PublishedEventReferenceId(Guid.CreateVersion7()),
            gaPoolId: new GeneralAdmissionPoolId(Guid.CreateVersion7()),
            checkedInAt: checkedInAt,
            orderId: new OrderId(Guid.CreateVersion7()),
            orderItemId: new OrderItemId(Guid.CreateVersion7()));

        Assert.True(record.Id != default);
        Assert.Equal(checkedInAt, record.CheckedInAt);
        Assert.False(record.InventorySeatId.HasValue);
        Assert.True(record.GeneralAdmissionPoolId.HasValue);
    }

    [Fact]
    public void CreateForSeat_WithEmptyTicketId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            AttendanceRecord.CreateForSeat(
                ticketId: new TicketId(Guid.Empty),
                ticketCode: new TicketCode("01ARZ3NDEKTSV4RR"),
                publishedEventId: new PublishedEventReferenceId(Guid.CreateVersion7()),
                inventorySeatId: new InventorySeatId(Guid.CreateVersion7()),
                checkedInAt: DateTimeOffset.UtcNow,
                orderId: new OrderId(Guid.CreateVersion7()),
                orderItemId: new OrderItemId(Guid.CreateVersion7())));

        Assert.Contains("Ticket ID cannot be empty", exception.Message);
    }

    [Fact]
    public void CreateForSeat_WithDefaultCheckedInAt_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            AttendanceRecord.CreateForSeat(
                ticketId: new TicketId(Guid.CreateVersion7()),
                ticketCode: new TicketCode("01ARZ3NDEKTSV4RR"),
                publishedEventId: new PublishedEventReferenceId(Guid.CreateVersion7()),
                inventorySeatId: new InventorySeatId(Guid.CreateVersion7()),
                checkedInAt: default,
                orderId: new OrderId(Guid.CreateVersion7()),
                orderItemId: new OrderItemId(Guid.CreateVersion7())));

        Assert.Contains("Checked-in timestamp cannot be the default value", exception.Message);
    }

    [Fact]
    public void CreateForSeat_WithEmptyInventorySeatId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            AttendanceRecord.CreateForSeat(
                ticketId: new TicketId(Guid.CreateVersion7()),
                ticketCode: new TicketCode("01ARZ3NDEKTSV4RR"),
                publishedEventId: new PublishedEventReferenceId(Guid.CreateVersion7()),
                inventorySeatId: new InventorySeatId(Guid.Empty),
                checkedInAt: DateTimeOffset.UtcNow,
                orderId: new OrderId(Guid.CreateVersion7()),
                orderItemId: new OrderItemId(Guid.CreateVersion7())));

        Assert.Contains("Inventory Seat ID cannot be empty", exception.Message);
    }

    [Fact]
    public void AttendanceRecordErrors_MissingAttendanceAssociation_ExposesExpectedCode()
    {
        var error = AttendanceRecordErrors.MissingAttendanceAssociation();

        Assert.Equal("Attendance.AttendanceRecord.MissingAttendanceAssociation", error.Code);
    }

    [Fact]
    public void AttendanceRecordErrors_InvalidAttendanceAssociation_ExposesExpectedCode()
    {
        var error = AttendanceRecordErrors.InvalidAttendanceAssociation(
            new InventorySeatId(Guid.CreateVersion7()),
            new GeneralAdmissionPoolId(Guid.CreateVersion7()));

        Assert.Equal("Attendance.AttendanceRecord.InvalidAttendanceAssociation", error.Code);
    }
}