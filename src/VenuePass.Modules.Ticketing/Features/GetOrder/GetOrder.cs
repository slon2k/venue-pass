using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.GetOrder;

public sealed record GetOrderQuery(Guid OrderId);

public sealed record GetOrderResult(
    Guid OrderId,
    Guid ReservationId,
    string Status,
    string Currency,
    decimal Total,
    string BuyerName,
    string BuyerEmail,
    IReadOnlyList<GetOrderItemResult> Items);

public sealed record GetOrderItemResult(
    Guid OrderItemId,
    string Type,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    Guid PriceZoneId,
    int Quantity,
    decimal UnitPrice,
    decimal Total);

public sealed class GetOrderHandler(TicketingDbContext db)
{
    public async Task<Result<GetOrderResult>> Handle(GetOrderQuery query, CancellationToken ct)
    {
        var orderId = new OrderId(query.OrderId);

        var order = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return GetOrderErrors.OrderNotFound(query.OrderId);
        }

        return new GetOrderResult(
            OrderId: order.Id.Value,
            ReservationId: order.ReservationId.Value,
            Status: order.Status.ToString(),
            Currency: order.Currency.Value,
            Total: order.Total.Value,
            BuyerName: order.BuyerName,
            BuyerEmail: order.BuyerEmail,
            Items: [.. order.Items.Select(i => new GetOrderItemResult(
                OrderItemId: i.Id.Value,
                Type: i.Type.ToString(),
                InventorySeatId: i.InventorySeatId?.Value,
                GeneralAdmissionPoolId: i.GeneralAdmissionPoolId?.Value,
                PriceZoneId: i.PriceZoneId.Value,
                Quantity: i.Quantity.Value,
                UnitPrice: i.UnitPrice.Value,
                Total: i.Total.Value))]);
    }
}
