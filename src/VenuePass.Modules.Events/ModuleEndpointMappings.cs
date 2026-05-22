using Microsoft.AspNetCore.Routing;
using VenuePass.Modules.Events.Features.CreateVenue;
using VenuePass.Modules.Events.Features.GetVenue;

namespace VenuePass.Modules.Events;

public static class ModuleEndpointMappings
{
    public static IEndpointRouteBuilder MapEventsModule(this IEndpointRouteBuilder app)
    {
        app.MapCreateVenue();
        app.MapGetVenue();

        return app;
    }
}