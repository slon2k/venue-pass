using Microsoft.AspNetCore.Routing;

using VenuePass.Modules.Ticketing.Features.ActivateOffer;
using VenuePass.Modules.Ticketing.Features.ConfigurePricing;
using VenuePass.Modules.Ticketing.Features.CreateOffer;
using VenuePass.Modules.Ticketing.Features.GetInventoryStatus;
using VenuePass.Modules.Ticketing.Features.GetOffer;
using VenuePass.Modules.Ticketing.Features.GetOffers;
using VenuePass.Modules.Ticketing.Features.CheckoutReservation;
using VenuePass.Modules.Ticketing.Features.CreateReservation;
using VenuePass.Modules.Ticketing.Features.GetOrder;
using VenuePass.Modules.Ticketing.Features.GetReservation;
using VenuePass.Modules.Ticketing.Features.CancelReservation;
using VenuePass.Modules.Ticketing.Features.GetTicket;

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
        app.MapGetInventoryStatus();
        app.MapCreateReservation();
        app.MapGetReservation();
        app.MapCancelReservation();
        app.MapCheckoutReservation();
        app.MapGetOrder();
        app.MapGetTicket();

        return app;
    }
}