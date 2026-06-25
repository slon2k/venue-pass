using FluentValidation;
using FluentValidation.Results;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Contracts;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.Reservations;
using VenuePass.Modules.Ticketing.Domain.Tickets;
using VenuePass.Modules.Ticketing.Infrastructure;
using VenuePass.Modules.Ticketing.Infrastructure.Outbox;

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
    IReadOnlyList<CheckoutReservationTicketResult> Tickets,
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

public sealed record CheckoutReservationTicketResult(
    Guid TicketId,
    string Code,
    Guid? InventorySeatId,
    Guid? GeneralAdmissionPoolId,
    DateTimeOffset CreatedAt);

public sealed class CheckoutReservationHandler(
    TicketingDbContext db,
    TicketIssuer ticketIssuer,
    IValidator<CheckoutReservationCommand> validator,
    TimeProvider timeProvider,
    ILogger<CheckoutReservationHandler> logger)
{
    private const int MaxTicketCodeCollisionRetries = 3;

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

        for (int attempt = 1; attempt <= MaxTicketCodeCollisionRetries + 1; attempt++)
        {
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

                var existingTickets = await db.Tickets
                    .AsNoTracking()
                    .Where(t => t.OrderId == existingOrder.Id)
                    .ToListAsync(ct);

                return ToResult(existingOrder, existingTickets, isNewOrder: false);
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

            try
            {
                var now = timeProvider.GetUtcNow();

                var order = Order.CreateFromReservation(reservation, command.BuyerName, command.BuyerEmail, now);

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
                var tickets = ticketIssuer.IssueTickets(inventory, order, now);

                var issuedCodes = tickets
                    .Select(t => t.Code.Value)
                    .ToArray();

                if (issuedCodes.Length != issuedCodes.Distinct(StringComparer.Ordinal).Count())
                {
                    logger.LogWarning(
                        "Duplicate ticket codes generated in-memory for reservation {ReservationId}; retrying {Attempt}/{MaxAttempts}.",
                        command.ReservationId,
                        attempt,
                        MaxTicketCodeCollisionRetries);

                    db.ChangeTracker.Clear();
                    continue;
                }

                var existingCodes = await db.Tickets
                    .AsNoTracking()
                    .Select(t => t.Code.Value)
                    .ToListAsync(ct);

                var existingCodeCollision = existingCodes.Any(issuedCodes.Contains);

                if (existingCodeCollision)
                {
                    logger.LogWarning(
                        "Generated ticket codes collided with existing tickets for reservation {ReservationId}; retrying {Attempt}/{MaxAttempts}.",
                        command.ReservationId,
                        attempt,
                        MaxTicketCodeCollisionRetries);

                    db.ChangeTracker.Clear();
                    continue;
                }

                foreach (var ticket in tickets)
                {
                    var integrationEvent = new TicketIssuedIntegrationEvent(
                        MessageId: Guid.NewGuid(),
                        TicketId: ticket.Id.Value,
                        TicketCode: ticket.Code.Value,
                        OrderId: order.Id.Value,
                        OrderItemId: ticket.OrderItemId.Value,
                        EventId: inventory.EventReferenceId.Value,
                        InventoryId: inventory.Id.Value,
                        OccurredOn: now);

                    db.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
                }

                db.Tickets.AddRange(tickets);
                await db.SaveChangesAsync(ct);

                return ToResult(order, tickets, isNewOrder: true);
            }
            catch (DomainException ex)
            {
                logger.LogInformation(ex, "Domain rule rejected checkout for reservation {ReservationId}.", command.ReservationId);
                return Error.FromDomainException(ex);
            }
            catch (DbUpdateException ex) when (IsTicketCodeUniqueViolation(ex) && attempt <= MaxTicketCodeCollisionRetries)
            {
                logger.LogWarning(
                    ex,
                    "Ticket code collision while checking out reservation {ReservationId}; retrying {Attempt}/{MaxAttempts}.",
                    command.ReservationId,
                    attempt,
                    MaxTicketCodeCollisionRetries);

                db.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogInformation(
                    "Concurrency conflict during checkout for reservation {ReservationId}.",
                    command.ReservationId);
                return CheckoutReservationErrors.ConcurrencyConflict();
            }
        }

        logger.LogError(
            "Ticket code collision retries exhausted for reservation {ReservationId}.",
            command.ReservationId);

        return CheckoutReservationErrors.ConcurrencyConflict();
    }

    private static bool IsTicketCodeUniqueViolation(DbUpdateException ex)
    {
        var sqlException = ex.InnerException as SqlException;

        if (sqlException is null)
        {
            return false;
        }

        // SQL Server: 2601 = unique index violation, 2627 = unique constraint violation
        if (sqlException.Number is not (2601 or 2627))
        {
            return false;
        }

        return sqlException.Message.Contains("IX_tickets_ticket_code", StringComparison.OrdinalIgnoreCase)
            || sqlException.Message.Contains("ticket_code", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<InventorySeatId> GetSeatIds(IReadOnlyList<ReservationItem> items) =>
        [.. items.Where(i => i.InventorySeatId.HasValue).Select(i => i.InventorySeatId!.Value)];

    private static IReadOnlyList<(GeneralAdmissionPoolId, Quantity)> GetGeneralAdmissionPools(IReadOnlyList<ReservationItem> items) =>
        [.. items.Where(i => i.GeneralAdmissionPoolId.HasValue)
            .Select(i => (i.GeneralAdmissionPoolId!.Value, i.Quantity))];

    private static CheckoutReservationResult ToResult(Order order, IReadOnlyList<Ticket> tickets, bool isNewOrder) =>
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
            Tickets: [.. tickets.Select(t => new CheckoutReservationTicketResult(
                TicketId: t.Id.Value,
                Code: t.Code.Value,
                InventorySeatId: t.InventorySeatId?.Value,
                GeneralAdmissionPoolId: t.GeneralAdmissionPoolId?.Value,
                CreatedAt: t.CreatedAt))],
            IsNewOrder: isNewOrder);
}
