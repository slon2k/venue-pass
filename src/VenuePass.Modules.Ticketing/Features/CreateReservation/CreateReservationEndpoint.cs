using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using VenuePass.BuildingBlocks.Application;
using static VenuePass.BuildingBlocks.Presentation.ErrorHttpMappingExtensions;

namespace VenuePass.Modules.Ticketing.Features.CreateReservation;

public static class CreateReservationEndpoint
{
    public sealed record CreateReservationRequest(
        Guid OfferId,
        IReadOnlyList<Guid>? SeatIds,
        IReadOnlyList<GeneralAdmissionPoolSelectionRequest>? GaPoolSelections);

    public sealed record GeneralAdmissionPoolSelectionRequest(
        Guid PoolId,
        int Quantity);

    public sealed record CreateReservationResponse(
        Guid ReservationId,
        string Status,
        DateTimeOffset ExpiresAt,
        string Currency,
        decimal Total,
        IReadOnlyList<CreateReservationItemResponse> Items);

    public sealed record CreateReservationItemResponse(
        Guid ReservationItemId,
        string Type,
        Guid? InventorySeatId,
        Guid? GeneralAdmissionPoolId,
        Guid PriceZoneId,
        int Quantity,
        decimal UnitPrice,
        decimal Total);

    public static IEndpointRouteBuilder MapCreateReservation(this IEndpointRouteBuilder app)
    {
        app.MapPost("/reservations", Handle)
            .WithName("CreateReservation")
            .WithTags("Reservations")
            .RequireAuthorization()
            .Produces<CreateReservationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        [FromBody] CreateReservationRequest request,
        CreateReservationHandler handler,
        CancellationToken ct)
    {
        var seats = request.SeatIds ?? [];
        var pools = request.GaPoolSelections ?? [];

        CreateReservationCommand command = new(
            OfferId: request.OfferId,
            SeatIds: seats,
            GeneralAdmissionPoolSelections: [.. pools.Select(p => new GeneralAdmissionPoolSelection(
                PoolId: p.PoolId,
                Quantity: p.Quantity))]);

        Result<CreateReservationResult> result = await handler.Handle(command, ct);

        return result.Match(ToCreated, ToProblem);
    }

    private static IResult ToCreated(CreateReservationResult result) =>
        Results.Created(
            $"/reservations/{result.ReservationId}",
                new CreateReservationResponse(
                    ReservationId: result.ReservationId,
                    Status: result.Status,
                    ExpiresAt: result.ExpiresAt,
                    Currency: result.Currency,
                    Total: result.Total,
                    Items: [.. result.Items.Select(i => new CreateReservationItemResponse(
                        ReservationItemId: i.ReservationItemId,
                        Type: i.Type,
                        InventorySeatId: i.InventorySeatId,
                        GeneralAdmissionPoolId: i.GeneralAdmissionPoolId,
                        PriceZoneId: i.PriceZoneId,
                        Quantity: i.Quantity,
                        UnitPrice: i.UnitPrice,
                        Total: i.Total))]));

}