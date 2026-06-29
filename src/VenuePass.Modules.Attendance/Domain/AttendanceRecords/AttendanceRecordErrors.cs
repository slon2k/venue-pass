using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Attendance.Domain.Common;

namespace VenuePass.Modules.Attendance.Domain;

public static class AttendanceRecordErrors
{
    public static DomainError TicketAlreadyMarkedAsAttended(Guid ticketId) => new(
            "Attendance.AttendanceRecord.TicketAlreadyMarkedAsAttended",
            $"Ticket with ID '{ticketId}' has already been marked as attended.");

    public static DomainError InvalidAttendanceAssociation(InventorySeatId inventorySeatId, GeneralAdmissionPoolId gaPoolId) => new(
            "Attendance.AttendanceRecord.InvalidAttendanceAssociation",
            $"An attendance record cannot be associated with both an inventory seat (ID: '{inventorySeatId.Value}') and a general admission pool (ID: '{gaPoolId.Value}').");

    public static DomainError MissingAttendanceAssociation() => new(
            "Attendance.AttendanceRecord.MissingAttendanceAssociation",
            "An attendance record must be associated with either an inventory seat or a general admission pool.");
}