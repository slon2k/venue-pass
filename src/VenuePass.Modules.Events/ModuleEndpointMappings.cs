using Microsoft.AspNetCore.Routing;
using VenuePass.Modules.Events.Features.CreateVenue;

namespace VenuePass.Modules.Events;

public static class ModuleEndpointMappings
{
    public static IEndpointRouteBuilder MapEventsModule(this IEndpointRouteBuilder app)
    {
        app.MapCreateVenue();

        return app;
    }
}