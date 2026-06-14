using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Ticketing.Domain.Reservations;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.GetReservation;

public record GetReservationQuery(Guid ReservationId);

public record GetReservationResult(
    Guid ReservationId,
    Guid OfferId,
    Guid InventoryId,
    string Status,
    DateTimeOffset ExpiresAt,
    string Currency,
    decimal Total,
    IReadOnlyList<GetReservationItemResult> Items);

public record GetReservationItemResult(
    Guid ReservationItemId,
    string Type,
    Guid PriceZoneId,    
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    decimal UnitPrice,    
    int Quantity,
    decimal Total);

public sealed class GetReservationHandler(TicketingDbContext db)
{
    public async Task<Result<GetReservationResult>> Handle(GetReservationQuery query, CancellationToken ct)
    {
        var reservationId = new ReservationId(query.ReservationId);

        var reservation = await db.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

        if (reservation is null)
        {
            return GetReservationErrors.ReservationNotFound(query.ReservationId);
        }

        var result = new GetReservationResult(
            ReservationId: reservation.Id.Value,
            OfferId: reservation.OfferId.Value,
            InventoryId: reservation.InventoryId.Value,
            Status: reservation.Status.ToString(),
            ExpiresAt: reservation.ExpiresAt,
            Currency: reservation.Currency.Value,
            Total: reservation.Total.Value,
            Items: [.. reservation.Items.Select(item => new GetReservationItemResult(
                ReservationItemId: item.Id.Value,
                Type: item.Type.ToString(),
                PriceZoneId: item.PriceZoneId.Value,
                InventorySeatId: item.InventorySeatId?.Value,
                GeneralAdmissionPoolId: item.GeneralAdmissionPoolId?.Value,
                UnitPrice: item.UnitPrice.Value,
                Quantity: item.Quantity.Value,
                Total: item.Total.Value))]
        );

        return Result.Success(result);
    }
}