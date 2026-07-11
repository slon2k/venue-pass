using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Attendance.Features.ScanTicket;

public static class ScanTicketEndpoint
{
    public sealed record ScanTicketRequest(
        string TicketCode,
        Guid PublishedEventReferenceId);

    public sealed record ScanTicketResponse(
        Guid AttendanceRecordId,
        Guid TicketId,
        string TicketCode,
        Guid PublishedEventReferenceId,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        Guid OrderId,
        Guid OrderItemId,
        DateTimeOffset CheckedInAt        
        );

    public static IEndpointRouteBuilder MapScanTicketEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/attendance/scans", Handle)
            .WithName("ScanTicket")
            .WithTags("Attendance")
            .RequireAuthorization("AttendanceOperator")
            .Produces<ScanTicketResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        ScanTicketRequest request,
        ScanTicketHandler scanTicketHandler,
        CancellationToken cancellationToken)
    {
        var result = await scanTicketHandler.HandleAsync(new ScanTicketCommand(request.TicketCode, request.PublishedEventReferenceId), cancellationToken);

        return result.Match(ToResponse, ToProblem);
    }

    private static IResult ToResponse(ScanTicketResult result) => Results.Created(
        $"/attendance/status?ticketId={result.TicketId}&publishedEventReferenceId={result.PublishedEventReferenceId}",
        new ScanTicketResponse(
            AttendanceRecordId: result.AttendanceRecordId,
            TicketId: result.TicketId,
            TicketCode: result.TicketCode,
            PublishedEventReferenceId: result.PublishedEventReferenceId,
            InventorySeatId: result.InventorySeatId,
            GeneralAdmissionPoolId: result.GeneralAdmissionPoolId,
            OrderId: result.OrderId,
            OrderItemId: result.OrderItemId,
            CheckedInAt: result.CheckedInAt));
}