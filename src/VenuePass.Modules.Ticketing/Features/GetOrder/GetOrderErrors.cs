using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Ticketing.Features.GetOrder;

public static class GetOrderErrors
{
    public static Error OrderNotFound(Guid orderId) => Error.NotFound(
        "Ticketing.GetOrder.OrderNotFound",
        $"No order found with ID {orderId}.");
}
