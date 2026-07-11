using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Attendance.Features.ScanTicket;

public static class ScanTicketErrors
{
    public static Error InvalidPublishedEventReferenceId() => Error.Validation(
        "Attendance.ScanTicket.InvalidPublishedEventReferenceId",
        "Published event reference id is required.");

    public static Error MalformedTicketCode(string ticketCode) => Error.Validation(
        "Attendance.ScanTicket.MalformedTicketCode",
        $"Malformed ticket code: {ticketCode}.");

    public static Error TicketNotFound(string ticketCode) => Error.NotFound(
        "Attendance.ScanTicket.TicketNotFound",
        $"No ticket found with code: {ticketCode}.");

    public static Error PublishedEventReferenceNotFound(Guid publishedEventReferenceId) => Error.NotFound(
        "Attendance.ScanTicket.PublishedEventReferenceNotFound",
        $"No published event reference found with ID: {publishedEventReferenceId}.");

    public static Error TicketCanceled(string ticketCode) => Error.Validation(
        "Attendance.ScanTicket.TicketCanceled",
        $"The ticket with code {ticketCode} has been canceled.");

    public static Error IncorrectEvent(string ticketCode, Guid publishedEventReferenceId) => Error.Validation(
        "Attendance.ScanTicket.IncorrectEvent",
        $"The ticket with code {ticketCode} is not valid for the published event reference with ID {publishedEventReferenceId}.");

    public static Error OtherFailure(string ticketCode, Guid publishedEventReferenceId) => Error.Unexpected(
        "Attendance.ScanTicket.OtherFailure",
        $"An unexpected error occurred while validating the ticket with code {ticketCode} for the published event reference with ID {publishedEventReferenceId}.");

    public static Error TicketAlreadyScanned(string ticketCode, Guid publishedEventReferenceId) => Error.Conflict(
        "Attendance.ScanTicket.TicketAlreadyScanned",
        $"The ticket with code {ticketCode} has already been scanned for the published event reference with ID {publishedEventReferenceId}.");
}