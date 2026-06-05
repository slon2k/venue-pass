using Microsoft.AspNetCore.Routing;

namespace VenuePass.Modules.Ticketing;

public static class ModuleEndpointMappings
{
    public static IEndpointRouteBuilder MapTicketingModule(this IEndpointRouteBuilder app)
    {
        // Map endpoints for the Ticketing module here, e.g.:
        // app.MapPost("/api/tickets", CreateTicketHandler.Handle).RequireAuthorization("EventManager");

        return app;
    }
}