using Microsoft.AspNetCore.Routing;

using VenuePass.Modules.Ticketing.Features.ActivateOffer;
using VenuePass.Modules.Ticketing.Features.ConfigurePricing;
using VenuePass.Modules.Ticketing.Features.CreateOffer;

namespace VenuePass.Modules.Ticketing;

public static class ModuleEndpointMappings
{
    public static IEndpointRouteBuilder MapTicketingModule(this IEndpointRouteBuilder app)
    {
        app.MapCreateOffer();
        app.MapConfigurePricing();
        app.MapActivateOffer();

        return app;
    }
}