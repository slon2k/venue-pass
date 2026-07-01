using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;

namespace VenuePass.Modules.Ticketing.Domain.Tickets;

public class TicketIssuer(ITicketCodeGenerator ticketCodeGenerator)
{
    private const int MaxUniqueGenerationAttemptsMultiplier = 10;

    private readonly ITicketCodeGenerator _ticketCodeGenerator = ticketCodeGenerator ?? throw new ArgumentNullException(nameof(ticketCodeGenerator));

    public IReadOnlyList<Ticket> IssueTickets(
        Inventory inventory,
        Order order,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));
        ArgumentNullException.ThrowIfNull(inventory, nameof(inventory));

        if (order.InventoryId != inventory.Id)
            throw new DomainRuleViolationException(TicketErrors.OrderInventoryMismatch(order.Id, order.InventoryId, inventory.Id));

         if (now == default)
            throw new ArgumentException("Current time cannot be the default value.", nameof(now));

        if (order.Items.Count == 0)
            throw new DomainRuleViolationException(TicketErrors.OrderHasNoItems(order.Id));
        
        if (order.TotalQuantity <= 0)
            throw new DomainRuleViolationException(TicketErrors.OrderHasInvalidTotalQuantity(order.Id, order.TotalQuantity));

        if (order.Status != OrderStatus.Completed)
            throw new DomainRuleViolationException(TicketErrors.OrderMustBeCompleted(order.Id));

        var tickets = new List<Ticket>(order.TotalQuantity);
        var issuedCodes = new HashSet<TicketCode>();

        foreach (var item in order.Items)
        {
            tickets.AddRange(CreateForOrderItem(
                eventReferenceId: inventory.EventReferenceId,
                inventoryId: inventory.Id,
                orderId: order.Id,
                orderItem: item,
                issuedCodes: issuedCodes,
                now: now));
        }
        
        return tickets.AsReadOnly();
    }

    private IReadOnlyList<Ticket> CreateForOrderItem(
    PublishedEventReferenceId eventReferenceId,
    InventoryId inventoryId,
    OrderId orderId,
    OrderItem orderItem,
    HashSet<TicketCode> issuedCodes,
    DateTimeOffset now) => orderItem.Type switch
    {
        OrderItemType.Seat => [Ticket.CreateForInventorySeat(
            publishedEventReferenceId: eventReferenceId,
            orderId: orderId,
            orderItemId: orderItem.Id,
            code: GenerateUniqueCode(issuedCodes),
            inventoryId: inventoryId,
            inventorySeatId: orderItem.InventorySeatId!.Value,
            now: now)],

        OrderItemType.GeneralAdmissionPool => Enumerable.Range(0, orderItem.Quantity.Value)
            .Select(_ => Ticket.CreateForGeneralAdmissionPool(
                publishedEventReferenceId: eventReferenceId,
                orderId: orderId,
                orderItemId: orderItem.Id,
                code: GenerateUniqueCode(issuedCodes),
                inventoryId: inventoryId,
                generalAdmissionPoolId: orderItem.GeneralAdmissionPoolId!.Value,
                now: now))
            .ToList(),

        _ => throw new ArgumentException($"Unsupported order item type '{orderItem.Type}'.", nameof(orderItem))
    };

    private TicketCode GenerateUniqueCode(HashSet<TicketCode> issuedCodes)
    {
        var attempts = 0;
        var maxAttempts = Math.Max(issuedCodes.Count + 1, 1) * MaxUniqueGenerationAttemptsMultiplier;

        while (attempts < maxAttempts)
        {
            var code = _ticketCodeGenerator.Generate();

            if (issuedCodes.Add(code))
            {
                return code;
            }

            attempts++;
        }

        throw new InvalidOperationException("Unable to generate distinct ticket codes within the maximum number of attempts.");
    }
}