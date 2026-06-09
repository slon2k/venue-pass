using Microsoft.AspNetCore.Routing;

using VenuePass.Modules.Ticketing.Features.CreateOffer;

namespace VenuePass.Modules.Ticketing;

public static class ModuleEndpointMappings
{
    public static IEndpointRouteBuilder MapTicketingModule(this IEndpointRouteBuilder app)
    {
        app.MapCreateOffer();

        return app;
    }
}