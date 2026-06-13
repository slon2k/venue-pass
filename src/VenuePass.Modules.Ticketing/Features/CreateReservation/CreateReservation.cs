using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.Reservations;
using VenuePass.Modules.Ticketing.Infrastructure;
using VenuePass.Modules.Ticketing.Options;

namespace VenuePass.Modules.Ticketing.Features.CreateReservation;

public sealed class CreateReservationHandler(
    TicketingDbContext db,
    IValidator<CreateReservationCommand> validator,
    IOptions<TicketingOptions> options,
    TimeProvider timeProvider,
    ILogger<CreateReservationHandler> logger)
{
    public async Task<Result<CreateReservationResult>> Handle(CreateReservationCommand command, CancellationToken ct)
    {
        var expirationTimeSpan = options.Value.ReservationExpiry;

        if (expirationTimeSpan <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"Invalid reservation expiry configured: {expirationTimeSpan}. Expiry must be a positive timespan.");
        }
        
        ValidationResult validationResult = await validator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            return CreateReservationErrors.InvalidData(
                [.. validationResult.Errors.Select(e =>
                    new ValidationErrorDetail(e.PropertyName, e.ErrorMessage))]);
        }

        var offerId = new OfferId(command.OfferId);

        var offer = await db.Offers
            .FirstOrDefaultAsync(o => o.Id == offerId, ct);

        if (offer is null)
        {
            return CreateReservationErrors.OfferNotFound(command.OfferId);
        }

        var inventory = await db.Inventories
            .Include(i => i.Seats)
            .Include(i => i.Pools)
            .FirstOrDefaultAsync(i => i.Id == offer.InventoryId, ct);

        if (inventory is null)
        {
            return CreateReservationErrors.InventoryNotFound(offer.InventoryId);
        }

        var seatInputs = command.SeatIds
            .Select(id => new ReservationItemInventorySeatInput(new InventorySeatId(id)))
            .ToArray();

        var poolInputs = command.GeneralAdmissionPoolSelections
            .Select(s => new ReservationItemGeneralAdmissionPoolInput(
                new GeneralAdmissionPoolId(s.PoolId),
                new Quantity(s.Quantity)))
            .ToArray();

        Reservation reservation;

        try
        {
            var now = timeProvider.GetUtcNow();
            var expiresAt = now.Add(expirationTimeSpan);

            reservation = Reservation.Create(
                offer,
                seatInputs,
                poolInputs,
                now,
                expiresAt);

            var seatReservations = GetSeatReservations(reservation.Items);

            if (seatReservations.Count > 0)
            {
                inventory.ReserveSeats(seatReservations);
            }

            foreach (var (poolId, quantity) in GetPoolReservations(reservation.Items))
            {
                inventory.ReserveGeneralAdmissionPool(poolId, quantity);
            }

            db.Reservations.Add(reservation);
            await db.SaveChangesAsync(ct);
        }
        catch (DomainException ex)
        {
            logger.LogInformation(ex, "Domain rule rejected reservation creation.");
            return Error.FromDomainException(ex);
        }
        catch (ArgumentException ex)
        {
            return CreateReservationErrors.InvalidData(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CreateReservationErrors.ConcurrencyConflict();
        }

        return new CreateReservationResult(
            ReservationId: reservation.Id,
            Status: reservation.Status.ToString(),
            ExpiresAt: reservation.ExpiresAt,
            Currency: offer.Currency.Value,
            Total: reservation.Total.Value,
            Items: [.. reservation.Items.Select(i => new CreateReservationItem(
                ReservationItemId: i.Id,
                Type: i.Type.ToString(),
                InventorySeatId: i.InventorySeatId?.Value,
                GeneralAdmissionPoolId: i.GeneralAdmissionPoolId?.Value,
                PriceZoneId: i.PriceZoneId.Value,
                Quantity: i.Quantity.Value,
                UnitPrice: i.UnitPrice.Value,
                Total: i.Total.Value))]);
    }

    private static IReadOnlyList<InventorySeatId> GetSeatReservations(IReadOnlyList<ReservationItem> items) =>
        [.. items.Where(i => i.InventorySeatId.HasValue).Select(i => i.InventorySeatId!.Value)];

    private static IReadOnlyList<(GeneralAdmissionPoolId, Quantity)> GetPoolReservations(IReadOnlyList<ReservationItem> items) =>
        [.. items.Where(i => i.GeneralAdmissionPoolId.HasValue).Select(i => (i.GeneralAdmissionPoolId!.Value, i.Quantity))];
}

public sealed record CreateReservationCommand(
    Guid OfferId,
    IReadOnlyList<Guid> SeatIds,
    IReadOnlyList<GeneralAdmissionPoolSelection> GeneralAdmissionPoolSelections);

public sealed record GeneralAdmissionPoolSelection(
    Guid PoolId,
    int Quantity);

public sealed record CreateReservationResult(
    Guid ReservationId,
    string Status,
    DateTimeOffset ExpiresAt,
    string Currency,
    decimal Total,
    IReadOnlyList<CreateReservationItem> Items);

public sealed record CreateReservationItem(
    Guid ReservationItemId,
    string Type,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    Guid PriceZoneId,
    int Quantity,
    decimal UnitPrice,
    decimal Total);