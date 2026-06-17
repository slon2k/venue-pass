using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.GetOrder;

public static class GetOrderEndpoint
{
    public sealed record GetOrderResponse(
        Guid OrderId,
        Guid ReservationId,
        string Status,
        string Currency,
        decimal Total,
        string BuyerName,
        string BuyerEmail,
        DateTimeOffset CreatedAt,
        IReadOnlyList<GetOrderItemResponse> Items,
        IReadOnlyList<GetOrderTicketResponse> Tickets);

    public sealed record GetOrderItemResponse(
        Guid OrderItemId,
        string Type,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        Guid PriceZoneId,
        int Quantity,
        decimal UnitPrice,
        decimal Total);

    public sealed record GetOrderTicketResponse(
        Guid TicketId,
        string Code,
        string Status,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        DateTimeOffset CreatedAt);

    public static IEndpointRouteBuilder MapGetOrder(this IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{orderId:guid}", Handle)
            .WithName("GetOrder")
            .WithTags("Orders")
            .RequireAuthorization()
            .Produces<GetOrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid orderId,
        GetOrderHandler handler,
        CancellationToken ct)
    {
        Result<GetOrderResult> result = await handler.Handle(new GetOrderQuery(orderId), ct);

        return result.Match(ToOk, ToProblem);
    }

    private static IResult ToOk(GetOrderResult result) =>
        Results.Ok(new GetOrderResponse(
            OrderId: result.OrderId,
            ReservationId: result.ReservationId,
            Status: result.Status,
            Currency: result.Currency,
            Total: result.Total,
            BuyerName: result.BuyerName,
            BuyerEmail: result.BuyerEmail,
            CreatedAt: result.CreatedAt,
            Items: [.. result.Items.Select(i => new GetOrderItemResponse(
                OrderItemId: i.OrderItemId,
                Type: i.Type,
                InventorySeatId: i.InventorySeatId,
                GeneralAdmissionPoolId: i.GeneralAdmissionPoolId,
                PriceZoneId: i.PriceZoneId,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice,
                Total: i.Total))],
            Tickets: [.. result.Tickets.Select(t => new GetOrderTicketResponse(
                TicketId: t.TicketId,
                Code: t.Code,
                Status: t.Status,
                InventorySeatId: t.InventorySeatId,
                GeneralAdmissionPoolId: t.GeneralAdmissionPoolId,
                CreatedAt: t.CreatedAt))]));
}
