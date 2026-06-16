using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.Reservations;
using VenuePass.Modules.Ticketing.Domain.Tickets;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.CheckoutReservation;

public sealed record CheckoutReservationCommand(
    Guid ReservationId,
    string BuyerName,
    string BuyerEmail);

public sealed record CheckoutReservationResult(
    Guid OrderId,
    Guid ReservationId,
    string Status,
    string Currency,
    decimal Total,
    string BuyerName,
    string BuyerEmail,
    IReadOnlyList<CheckoutReservationItemResult> Items,
    bool IsNewOrder);

public sealed record CheckoutReservationItemResult(
    Guid OrderItemId,
    string Type,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    Guid PriceZoneId,
    int Quantity,
    decimal UnitPrice,
    decimal Total);

public sealed class CheckoutReservationHandler(
    TicketingDbContext db,
    TicketIssuer ticketIssuer,
    IValidator<CheckoutReservationCommand> validator,
    TimeProvider timeProvider,
    ILogger<CheckoutReservationHandler> logger)
{
    public async Task<Result<CheckoutReservationResult>> Handle(
        CheckoutReservationCommand command,
        CancellationToken ct)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            return CheckoutReservationErrors.InvalidData(
                [.. validationResult.Errors.Select(e =>
                    new ValidationErrorDetail(e.PropertyName, e.ErrorMessage))]);
        }

        var reservationId = new ReservationId(command.ReservationId);

        var reservation = await db.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

        if (reservation is null)
        {
            return CheckoutReservationErrors.ReservationNotFound(command.ReservationId);
        }

        // Idempotency: completed reservation with an existing order → return it
        if (reservation.Status == ReservationStatus.Completed)
        {
            var existingOrder = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.ReservationId == reservationId, ct);

            if (existingOrder is null)
            {
                logger.LogError(
                    "Reservation {ReservationId} is Completed but no order was found.",
                    reservation.Id);

                return CheckoutReservationErrors.OrderNotFound(command.ReservationId);
            }

            return ToResult(existingOrder, isNewOrder: false);
        }

        var inventory = await db.Inventories
            .Include(i => i.Seats)
            .Include(i => i.Pools)
            .FirstOrDefaultAsync(i => i.Id == reservation.InventoryId, ct);

        if (inventory is null)
        {
            logger.LogError(
                "Inventory {InventoryId} referenced by reservation {ReservationId} was not found.",
                reservation.InventoryId, reservation.Id);

            return CheckoutReservationErrors.InventoryNotFound(reservation.InventoryId.Value);
        }

        Order order;

        try
        {
            var now = timeProvider.GetUtcNow();

            order = Order.CreateFromReservation(reservation, command.BuyerName, command.BuyerEmail, now);

            reservation.Complete(now);

            var seatIds = GetSeatIds(reservation.Items);

            if (seatIds.Count > 0)
            {
                inventory.SellSeats(seatIds);
            }

            foreach (var (poolId, quantity) in GetGeneralAdmissionPools(reservation.Items))
            {
                inventory.SellGeneralAdmissionPool(poolId, quantity);
            }

            db.Orders.Add(order);
            var tickets = ticketIssuer.IssueTickets(order, now);
            db.Tickets.AddRange(tickets);
            await db.SaveChangesAsync(ct);
        }
        catch (DomainException ex)
        {
            logger.LogInformation(ex, "Domain rule rejected checkout for reservation {ReservationId}.", command.ReservationId);
            return Error.FromDomainException(ex);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation(
                "Concurrency conflict during checkout for reservation {ReservationId}.",
                command.ReservationId);
            return CheckoutReservationErrors.ConcurrencyConflict();
        }

        return ToResult(order, isNewOrder: true);
    }

    private static IReadOnlyList<InventorySeatId> GetSeatIds(IReadOnlyList<ReservationItem> items) =>
        [.. items.Where(i => i.InventorySeatId.HasValue).Select(i => i.InventorySeatId!.Value)];

    private static IReadOnlyList<(GeneralAdmissionPoolId, Quantity)> GetGeneralAdmissionPools(IReadOnlyList<ReservationItem> items) =>
        [.. items.Where(i => i.GeneralAdmissionPoolId.HasValue)
            .Select(i => (i.GeneralAdmissionPoolId!.Value, i.Quantity))];

    private static CheckoutReservationResult ToResult(Order order, bool isNewOrder) =>
        new(
            OrderId: order.Id.Value,
            ReservationId: order.ReservationId.Value,
            Status: order.Status.ToString(),
            Currency: order.Currency.Value,
            Total: order.Total.Value,
            BuyerName: order.BuyerName,
            BuyerEmail: order.BuyerEmail,
            Items: [.. order.Items.Select(i => new CheckoutReservationItemResult(
                OrderItemId: i.Id.Value,
                Type: i.Type.ToString(),
                InventorySeatId: i.InventorySeatId?.Value,
                GeneralAdmissionPoolId: i.GeneralAdmissionPoolId?.Value,
                PriceZoneId: i.PriceZoneId.Value,
                Quantity: i.Quantity.Value,
                UnitPrice: i.UnitPrice.Value,
                Total: i.Total.Value))],
            IsNewOrder: isNewOrder);
}
