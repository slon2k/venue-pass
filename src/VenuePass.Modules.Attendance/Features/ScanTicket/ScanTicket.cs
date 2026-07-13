using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Attendance.Contracts;
using VenuePass.Modules.Attendance.Domain.AttendanceRecords;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.ScanAttempts;
using VenuePass.Modules.Attendance.Infrastructure;
using VenuePass.Modules.Attendance.Infrastructure.Outbox;
using VenuePass.Modules.Ticketing.Contracts;

namespace VenuePass.Modules.Attendance.Features.ScanTicket;

public sealed record ScanTicketCommand(
    string TicketCode,
    Guid PublishedEventReferenceId);

public sealed record ScanTicketResult(
    Guid AttendanceRecordId,
    Guid TicketId,
    string TicketCode,
    Guid PublishedEventReferenceId,
    DateTimeOffset CheckedInAt,
    Guid OrderId,
    Guid OrderItemId,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId);

public sealed class ScanTicketHandler(
    AttendanceDbContext dbContext,
    ITicketingModuleContract ticketingContract,
    TimeProvider timeProvider,
    ILogger<ScanTicketHandler> logger
    )
{
    public async Task<Result<ScanTicketResult>> HandleAsync(ScanTicketCommand command, CancellationToken cancellationToken = default)
    {
        if (command.PublishedEventReferenceId == Guid.Empty)
        {
            return ScanTicketErrors.InvalidPublishedEventReferenceId();
        }

        var checkedInAt = timeProvider.GetUtcNow();

        if (!TicketCode.TryCreate(command.TicketCode, out var validTicketCode))
        {
            var scanAttempt = ScanAttempt.MalformedTicketCode(
                new PublishedEventReferenceId(command.PublishedEventReferenceId),
                new SubmittedTicketCode(command.TicketCode),
                checkedInAt);
            await RecordScanAttempt(scanAttempt, cancellationToken);
            return ScanTicketErrors.MalformedTicketCode(command.TicketCode);
        }

        TicketValidationResultDto ticketValidationResult;

        try
        {
            ticketValidationResult = await ticketingContract.ValidateTicketForPublishedEventReferenceAsync(
                validTicketCode.Value,
                command.PublishedEventReferenceId,
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
                return await HandleValidationUnavailableAsync(command, validTicketCode, checkedInAt, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(
                ex,
                "Ticket validation timed out for TicketCode: {TicketCode}, PublishedEventReferenceId: {PublishedEventReferenceId}",
                validTicketCode.Value,
                command.PublishedEventReferenceId);

                return await HandleValidationUnavailableAsync(command, validTicketCode, checkedInAt, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Ticket validation failed for TicketCode: {TicketCode}, PublishedEventReferenceId: {PublishedEventReferenceId}",
                validTicketCode.Value,
                command.PublishedEventReferenceId);

                return await HandleValidationUnavailableAsync(command, validTicketCode, checkedInAt, cancellationToken);
        }

        if (!ticketValidationResult.IsValid)
        {
            var scanAttempt = ToRejectedScanAttempt(command, ticketValidationResult, validTicketCode, checkedInAt);
            await RecordScanAttempt(scanAttempt, cancellationToken);
            return ToError(ticketValidationResult, validTicketCode.Value, command.PublishedEventReferenceId);
        }

        var ticketDto = ticketValidationResult?.Ticket ?? throw new InvalidOperationException("Ticket validation result is valid but does not contain ticket information.");

        var ticketId = new TicketId(ticketDto.TicketId);

        if (await dbContext.AttendanceRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(ar => ar.TicketId == ticketId, cancellationToken) is AttendanceRecord existingAttendanceRecord)
        {
            var scanAttempt = ScanAttempt.DuplicateScan(
                new SubmittedTicketCode(command.TicketCode),
                new TicketCode(existingAttendanceRecord.TicketCode.Value),
                new TicketId(existingAttendanceRecord.TicketId.Value),
                new PublishedEventReferenceId(command.PublishedEventReferenceId),
                checkedInAt);
            await RecordScanAttempt(scanAttempt, cancellationToken);
            return ScanTicketErrors.TicketAlreadyScanned(validTicketCode.Value, command.PublishedEventReferenceId);
        }

        try
        {
            var attendanceRecord = FromTicketDto(ticketDto, checkedInAt);

            var integrationEvent = new TicketCheckedInIntegrationEvent(
                MessageId: Guid.NewGuid(),
                TicketId: attendanceRecord.TicketId.Value,
                TicketCode: attendanceRecord.TicketCode.Value,
                PublishedEventId: attendanceRecord.PublishedEventReferenceId.Value,
                InventorySeatId: attendanceRecord.InventorySeatId?.Value,
                GeneralAdmissionPoolId: attendanceRecord.GeneralAdmissionPoolId?.Value,
                OrderId: attendanceRecord.OrderId.Value,
                OrderItemId: attendanceRecord.OrderItemId.Value,
                OccurredOn: checkedInAt);

            var scanAttempt = ScanAttempt.Accepted(
                new SubmittedTicketCode(command.TicketCode),
                new TicketCode(attendanceRecord.TicketCode.Value),
                new TicketId(attendanceRecord.TicketId.Value),
                new PublishedEventReferenceId(attendanceRecord.PublishedEventReferenceId.Value),
                checkedInAt);

            dbContext.AttendanceRecords.Add(attendanceRecord);
            dbContext.ScanAttempts.Add(scanAttempt);
            dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

            await dbContext.SaveChangesAsync(cancellationToken);

            return new ScanTicketResult(
                attendanceRecord.Id,
                attendanceRecord.TicketId,
                attendanceRecord.TicketCode.Value,
                attendanceRecord.PublishedEventReferenceId,
                attendanceRecord.CheckedInAt,
                attendanceRecord.OrderId,
                attendanceRecord.OrderItemId,
                attendanceRecord.InventorySeatId,
                attendanceRecord.GeneralAdmissionPoolId);
        }
        catch (DomainException ex)
        {
            return Error.FromDomainException(ex);
        }
        catch (DbUpdateException ex) when (IsDuplicateAttendanceRecord(ex))
        {
            dbContext.ChangeTracker.Clear();

            var existingRecord = await dbContext.AttendanceRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(ar => ar.TicketId == ticketId, cancellationToken);

            if (existingRecord is not null)
            {
                var scanAttempt = ScanAttempt.DuplicateScan(
                    new SubmittedTicketCode(command.TicketCode),
                    new TicketCode(existingRecord.TicketCode.Value),
                    new TicketId(existingRecord.TicketId.Value),
                    new PublishedEventReferenceId(command.PublishedEventReferenceId),
                    checkedInAt);
                await RecordScanAttempt(scanAttempt, cancellationToken);

                return ScanTicketErrors.TicketAlreadyScanned(
                    validTicketCode.Value,
                    command.PublishedEventReferenceId);
            }

            var existingByCode = await dbContext.AttendanceRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(ar => ar.TicketCode == validTicketCode, cancellationToken);

            if (existingByCode is not null)
            {
                logger.LogError(
                    "Attendance record unique TicketCode conflict detected, but TicketId did not match validation result. TicketId: {TicketId}, ExistingTicketId: {ExistingTicketId}, TicketCode: {TicketCode}, PublishedEventReferenceId: {PublishedEventReferenceId}",
                    ticketId.Value,
                    existingByCode.TicketId.Value,
                    validTicketCode.Value,
                    command.PublishedEventReferenceId);

                throw new InvalidOperationException(
                    "Attendance record contract violation: TicketCode exists with different TicketId.");
            }

            logger.LogError(
                ex,
                "Duplicate attendance record constraint violation occurred, but no existing attendance record was found. TicketId: {TicketId}, TicketCode: {TicketCode}, PublishedEventReferenceId: {PublishedEventReferenceId}",
                ticketId.Value,
                validTicketCode.Value,
                command.PublishedEventReferenceId);

            throw;
        }
    }

    private static bool IsDuplicateAttendanceRecord(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;

        return ex.InnerException is SqlException sqlException
            && sqlException.Number is 2601 or 2627
            && (
                message.Contains("IX_attendance_records_ticket_id", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("IX_attendance_records_ticket_code", StringComparison.OrdinalIgnoreCase)
            );
    }

    private async Task RecordScanAttempt(ScanAttempt scanAttempt, CancellationToken cancellationToken)
    {
        dbContext.ScanAttempts.Add(scanAttempt);
        await dbContext.SaveChangesAsync(cancellationToken);  
    }

    private static AttendanceRecord FromTicketDto(TicketExportDto ticketDto, DateTimeOffset checkedInAt)
    {
        return (ticketDto.InventorySeatId, ticketDto.GeneralAdmissionPoolId) switch
        {
            (Guid inventorySeatId, null) => AttendanceRecord.CreateForSeat(
                new TicketId(ticketDto.TicketId),
                new TicketCode(ticketDto.TicketCode),
                new PublishedEventReferenceId(ticketDto.PublishedEventReferenceId),
                new InventorySeatId(inventorySeatId),
                checkedInAt,
                new OrderId(ticketDto.OrderId),
                new OrderItemId(ticketDto.OrderItemId)),

            (null, Guid generalAdmissionPoolId) => AttendanceRecord.CreateForGeneralAdmission(
                new TicketId(ticketDto.TicketId),
                new TicketCode(ticketDto.TicketCode),
                new PublishedEventReferenceId(ticketDto.PublishedEventReferenceId),
                new GeneralAdmissionPoolId(generalAdmissionPoolId),
                checkedInAt,
                new OrderId(ticketDto.OrderId),
                new OrderItemId(ticketDto.OrderItemId)),

            _ => throw new InvalidOperationException("Ticket must have either an InventorySeatId or a GeneralAdmissionPoolId, but not both.")
        }   ;
    }

    private static Error ToError(TicketValidationResultDto ticketValidationResult, string ticketCode, Guid publishedEventReferenceId)
    {
        if (ticketValidationResult.IsValid)
        {
            throw new InvalidOperationException("Cannot convert a valid ticket validation result to an error result.");
        }

        return ticketValidationResult.FailureReason switch
        {
            TicketValidationFailureReason.MalformedTicketCode => ScanTicketErrors.MalformedTicketCode(ticketCode),
            TicketValidationFailureReason.TicketNotFound => ScanTicketErrors.UnknownTicket(ticketCode),
            TicketValidationFailureReason.PublishedEventReferenceNotFound => ScanTicketErrors.PublishedEventReferenceNotFound(publishedEventReferenceId),
            TicketValidationFailureReason.TicketCanceled => ScanTicketErrors.TicketCanceled(ticketCode),
            TicketValidationFailureReason.IncorrectEvent => ScanTicketErrors.IncorrectEvent(ticketCode, publishedEventReferenceId),
            TicketValidationFailureReason.InvalidTicket => ScanTicketErrors.InvalidTicket(ticketCode, publishedEventReferenceId),
            _ => ScanTicketErrors.OtherFailure(ticketCode, publishedEventReferenceId)
        };
    }

    private static ScanAttempt ToRejectedScanAttempt(
        ScanTicketCommand command,
        TicketValidationResultDto ticketValidationResult,
        TicketCode normalizedTicketCode,
        DateTimeOffset checkedInAt)
    {
        return ticketValidationResult.FailureReason switch
        {
            TicketValidationFailureReason.MalformedTicketCode => ScanAttempt.MalformedTicketCode(
                new PublishedEventReferenceId(command.PublishedEventReferenceId),
                new SubmittedTicketCode(command.TicketCode),
                checkedInAt),

            TicketValidationFailureReason.PublishedEventReferenceNotFound => ScanAttempt.PublishedEventReferenceNotFound(
                new PublishedEventReferenceId(command.PublishedEventReferenceId),
                new SubmittedTicketCode(command.TicketCode),
                checkedInAt),

            TicketValidationFailureReason.TicketNotFound => ScanAttempt.UnknownTicket(
                new SubmittedTicketCode(command.TicketCode),
                normalizedTicketCode,
                new PublishedEventReferenceId(command.PublishedEventReferenceId),
                checkedInAt),

            TicketValidationFailureReason.InvalidTicket => ScanAttempt.InvalidTicket(
                new SubmittedTicketCode(command.TicketCode),
                normalizedTicketCode,
                new TicketId(ticketValidationResult.Ticket?.TicketId ?? throw new InvalidOperationException("Ticket validation result is invalid but does not contain ticket information.")),
                new PublishedEventReferenceId(command.PublishedEventReferenceId),
                checkedInAt),
            
            TicketValidationFailureReason.TicketCanceled => ScanAttempt.CanceledTicket(
                new SubmittedTicketCode(command.TicketCode),
                normalizedTicketCode,
                new TicketId(ticketValidationResult.Ticket?.TicketId ?? throw new InvalidOperationException("Ticket validation result is invalid but does not contain ticket information.")),
                new PublishedEventReferenceId(command.PublishedEventReferenceId),
                checkedInAt),

            TicketValidationFailureReason.IncorrectEvent => ScanAttempt.IncorrectEvent(
                new SubmittedTicketCode(command.TicketCode),
                normalizedTicketCode,
                new TicketId(ticketValidationResult.Ticket?.TicketId ?? throw new InvalidOperationException("Ticket validation result is invalid but does not contain ticket information.")),
                new PublishedEventReferenceId(command.PublishedEventReferenceId),
                checkedInAt),

            _ => ScanAttempt.UnexpectedError(
                new SubmittedTicketCode(command.TicketCode),
                normalizedTicketCode,
                new PublishedEventReferenceId(command.PublishedEventReferenceId),
                checkedInAt)
        };
    }

    private async Task<Result<ScanTicketResult>> HandleValidationUnavailableAsync(
        ScanTicketCommand command,
        TicketCode normalizedTicketCode,
        DateTimeOffset checkedInAt,
        CancellationToken cancellationToken)
    {
        var scanAttempt = ScanAttempt.ValidationUnavailable(
            new SubmittedTicketCode(command.TicketCode),
            normalizedTicketCode,
            new PublishedEventReferenceId(command.PublishedEventReferenceId),
            checkedInAt);

        await RecordScanAttempt(scanAttempt, cancellationToken);

        return ScanTicketErrors.ValidationUnavailable(command.TicketCode, command.PublishedEventReferenceId);
    }
}