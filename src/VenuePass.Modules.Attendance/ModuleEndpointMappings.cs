using Microsoft.AspNetCore.Routing;

using VenuePass.Modules.Attendance.Features.ScanTicket;

namespace VenuePass.Modules.Attendance;

public static class ModuleEndpointMappings
{
    public static IEndpointRouteBuilder MapAttendanceModule(this IEndpointRouteBuilder app)
    {
        app.MapScanTicketEndpoint();

        return app;
    }

}