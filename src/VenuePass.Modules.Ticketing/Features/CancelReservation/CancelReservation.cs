using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Reservations;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.CancelReservation;

public sealed record CancelReservationCommand(Guid ReservationId);

public sealed class CancelReservationHandler(
    TicketingDbContext db,
    ILogger<CancelReservationHandler> logger)
{
    public async Task<Result> Handle(
        CancelReservationCommand command,
        CancellationToken ct)
    {
        var reservationId = new ReservationId(command.ReservationId);

        var reservation = await db.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

        if (reservation is null)
        {
            return CancelReservationErrors.ReservationNotFound(command.ReservationId);
        }

       var inventory = await db.Inventories
            .FirstOrDefaultAsync(i => i.Id == reservation.InventoryId, ct);

        if (inventory is null)
        {
            logger.LogError(
                "Inventory with ID {InventoryId} referenced by reservation with ID {ReservationId} was not found.",
                reservation.InventoryId, reservation.Id);
            
            return CancelReservationErrors.InventoryNotFound(reservation.InventoryId.Value);
        }

        try
        {
            reservation.Cancel();

            var seatsToRelease = GetSeatReservations(reservation.Items);
            var poolsToRelease = GetPoolReservations(reservation.Items);

            if (seatsToRelease.Count > 0)
            {
                inventory.ReleaseSeats(seatsToRelease);
            }

            foreach (var (poolId, quantity) in poolsToRelease)
            {
                inventory.ReleaseGeneralAdmissionPool(poolId, quantity);
            }   

            await db.SaveChangesAsync(ct);
        }
        catch (DomainException ex)
        {
            logger.LogInformation(ex, "Domain rule rejected reservation cancellation.");
            return Error.FromDomainException(ex);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation("Concurrency conflict occurred while cancelling reservation with ID {ReservationId}.", command.ReservationId);
            return CancelReservationErrors.ConcurrencyConflict();
        }

        return Result.Success();
    }

    private static IReadOnlyList<InventorySeatId> GetSeatReservations(IReadOnlyList<ReservationItem> items) =>
        [.. items.Where(i => i.InventorySeatId.HasValue).Select(i => i.InventorySeatId!.Value)];

    private static IReadOnlyList<(GeneralAdmissionPoolId, Quantity)> GetPoolReservations(IReadOnlyList<ReservationItem> items) =>
        [.. items.Where(i => i.GeneralAdmissionPoolId.HasValue).Select(i => (i.GeneralAdmissionPoolId!.Value, i.Quantity))];

}