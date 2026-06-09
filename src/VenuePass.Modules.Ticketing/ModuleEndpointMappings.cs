using Microsoft.AspNetCore.Routing;

using VenuePass.Modules.Ticketing.Features.ActivateOffer;
using VenuePass.Modules.Ticketing.Features.ConfigurePricing;
using VenuePass.Modules.Ticketing.Features.CreateOffer;
using VenuePass.Modules.Ticketing.Features.GetOffer;
using VenuePass.Modules.Ticketing.Features.GetOffers;

namespace VenuePass.Modules.Ticketing;

public static class ModuleEndpointMappings
{
    public static IEndpointRouteBuilder MapTicketingModule(this IEndpointRouteBuilder app)
    {
        app.MapCreateOffer();
        app.MapConfigurePricing();
        app.MapActivateOffer();
        app.MapGetOffer();
        app.MapGetOffers();

        return app;
    }
}