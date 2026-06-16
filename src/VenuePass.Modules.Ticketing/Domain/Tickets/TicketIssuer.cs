using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Ticketing.Domain.Orders;

namespace VenuePass.Modules.Ticketing.Domain.Tickets;

public class TicketIssuer(ITicketCodeGenerator ticketCodeGenerator)
{
    private readonly ITicketCodeGenerator _ticketCodeGenerator = ticketCodeGenerator ?? throw new ArgumentNullException(nameof(ticketCodeGenerator));

    public IReadOnlyList<Ticket> IssueTickets(
        Order order,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));

        if (now == default)
            throw new ArgumentException("Current time cannot be the default value.", nameof(now));

        if (order.Items.Count == 0)
            throw new DomainRuleViolationException(TicketErrors.OrderHasNoItems(order.Id));
        
        if (order.TotalQuantity <= 0)
            throw new DomainRuleViolationException(TicketErrors.OrderHasInvalidTotalQuantity(order.Id, order.TotalQuantity));

        if (order.Status != OrderStatus.Completed)
            throw new DomainRuleViolationException(TicketErrors.OrderMustBeCompleted(order.Id));

        var tickets = new List<Ticket>();

        var codeEnumerator = GenerateDistinctBatch(order.TotalQuantity).GetEnumerator();

        foreach (var item in order.Items)
        {
            tickets.AddRange(CreateForOrderItem(
                orderId: order.Id,
                orderItem: item,
                codeEnumerator: codeEnumerator,
                now: now));
        }
        
        return tickets.AsReadOnly();
    }

    private static IReadOnlyList<Ticket> CreateForOrderItem(
    OrderId orderId,
    OrderItem orderItem,
    IEnumerator<TicketCode> codeEnumerator,
    DateTimeOffset now) => orderItem.Type switch
    {
        OrderItemType.Seat => [Ticket.CreateForInventorySeat(
            orderId: orderId,
            orderItemId: orderItem.Id,
            code: codeEnumerator.MoveNext() ? codeEnumerator.Current : throw new InvalidOperationException("Not enough ticket codes generated for the order items."),
            inventorySeatId: orderItem.InventorySeatId!.Value,
            now: now)],

        OrderItemType.GeneralAdmissionPool => Enumerable.Range(0, orderItem.Quantity.Value)
            .Select(_ => Ticket.CreateForGeneralAdmissionPool(
                orderId: orderId,
                orderItemId: orderItem.Id,
                code: codeEnumerator.MoveNext() ? codeEnumerator.Current : throw new InvalidOperationException("Not enough ticket codes generated for the order items."),
                generalAdmissionPoolId: orderItem.GeneralAdmissionPoolId!.Value,
                now: now))
            .ToList(),

        _ => throw new ArgumentException($"Unsupported order item type '{orderItem.Type}'.", nameof(orderItem))
    };

    private HashSet<TicketCode> GenerateDistinctBatch(int quantity)
    {
        HashSet<TicketCode> codes = [];

        var attempts = 0;
        var maxAttempts = quantity * 10;

        while (codes.Count < quantity)
        {
            if (attempts >= maxAttempts)
            {
                throw new InvalidOperationException("Unable to generate distinct ticket codes within the maximum number of attempts.");
            }

            codes.Add(_ticketCodeGenerator.Generate());
            attempts++;
        }

        return codes;
    }
}