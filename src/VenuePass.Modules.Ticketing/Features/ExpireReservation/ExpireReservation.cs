using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Reservations;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.ExpireReservation;

public sealed record ExpireReservationCommand(Guid ReservationId);

public sealed class ExpireReservationHandler(
    TicketingDbContext db,
    TimeProvider timeProvider,
    ILogger<ExpireReservationHandler> logger)
{
    public async Task<Result> Handle(ExpireReservationCommand command, CancellationToken ct)
    {
        Reservation? reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == new ReservationId(command.ReservationId), ct);

        if (reservation is null)
        {
            return ExpireReservationErrors.ReservationNotFound(command.ReservationId);
        }

       var inventory = await db.Inventories
            .Include(i => i.Seats)
            .Include(i => i.Pools)
            .FirstOrDefaultAsync(i => i.Id == reservation.InventoryId, ct);

        if (inventory is null)
        {
            logger.LogError(
                "Inventory with ID {InventoryId} referenced by reservation with ID {ReservationId} was not found.",
                reservation.InventoryId, reservation.Id);
            
            return ExpireReservationErrors.InventoryNotFound(reservation.InventoryId.Value);
        }        

        try
        {
            reservation.Expire(timeProvider.GetUtcNow());

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
            logger.LogInformation(ex, "Domain rule rejected reservation expiration.");
            return Error.FromDomainException(ex);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogInformation(ex, "Concurrency conflict occurred while expiring reservation.");
            return ExpireReservationErrors.ConcurrencyConflict(reservation.Id.Value);
        }

        return Result.Success();
    }

    private static IReadOnlyList<InventorySeatId> GetSeatReservations(IReadOnlyList<ReservationItem> items) =>
        [.. items.Where(i => i.InventorySeatId.HasValue).Select(i => i.InventorySeatId!.Value)];

    private static IReadOnlyList<(GeneralAdmissionPoolId, Quantity)> GetPoolReservations(IReadOnlyList<ReservationItem> items) =>
        [.. items.Where(i => i.GeneralAdmissionPoolId.HasValue).Select(i => (i.GeneralAdmissionPoolId!.Value, i.Quantity))];

}