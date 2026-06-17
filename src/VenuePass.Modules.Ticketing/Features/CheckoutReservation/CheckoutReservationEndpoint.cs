using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.CheckoutReservation;

public static class CheckoutReservationEndpoint
{
    public sealed record CheckoutReservationRequest(
        string BuyerName,
        string BuyerEmail);

    public sealed record CheckoutReservationResponse(
        Guid OrderId,
        Guid ReservationId,
        string Status,
        string Currency,
        decimal Total,
        string BuyerName,
        string BuyerEmail,
        IReadOnlyList<CheckoutReservationItemResponse> Items,
        IReadOnlyList<CheckoutReservationTicketResponse> Tickets);

    public sealed record CheckoutReservationItemResponse(
        Guid OrderItemId,
        string Type,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        Guid PriceZoneId,
        int Quantity,
        decimal UnitPrice,
        decimal Total);

    public sealed record CheckoutReservationTicketResponse(
        Guid TicketId,
        string Code,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        DateTimeOffset CreatedAt);

    public static IEndpointRouteBuilder MapCheckoutReservation(this IEndpointRouteBuilder app)
    {
        app.MapPost("/reservations/{reservationId:guid}/checkout", Handle)
            .WithName("CheckoutReservation")
            .WithTags("Reservations")
            .RequireAuthorization()
            .Produces<CheckoutReservationResponse>(StatusCodes.Status201Created)
            .Produces<CheckoutReservationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid reservationId,
        [FromBody] CheckoutReservationRequest request,
        CheckoutReservationHandler handler,
        CancellationToken ct)
    {
        var command = new CheckoutReservationCommand(
            ReservationId: reservationId,
            BuyerName: request.BuyerName,
            BuyerEmail: request.BuyerEmail);

        Result<CheckoutReservationResult> result = await handler.Handle(command, ct);

        return result.Match(ToResponse, ToProblem);
    }

    private static IResult ToResponse(CheckoutReservationResult result)
    {
        var response = new CheckoutReservationResponse(
            OrderId: result.OrderId,
            ReservationId: result.ReservationId,
            Status: result.Status,
            Currency: result.Currency,
            Total: result.Total,
            BuyerName: result.BuyerName,
            BuyerEmail: result.BuyerEmail,
            Items: [.. result.Items.Select(i => new CheckoutReservationItemResponse(
                OrderItemId: i.OrderItemId,
                Type: i.Type,
                InventorySeatId: i.InventorySeatId,
                GeneralAdmissionPoolId: i.GeneralAdmissionPoolId,
                PriceZoneId: i.PriceZoneId,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice,
                Total: i.Total))],
            Tickets: [.. result.Tickets.Select(t => new CheckoutReservationTicketResponse(
                TicketId: t.TicketId,
                Code: t.Code,
                InventorySeatId: t.InventorySeatId,
                GeneralAdmissionPoolId: t.GeneralAdmissionPoolId,
                CreatedAt: t.CreatedAt))]);

        return result.IsNewOrder
            ? Results.Created($"/orders/{result.OrderId}", response)
            : Results.Ok(response);
    }
}
