using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Orders;

namespace VenuePass.Modules.Ticketing.Domain.Tickets;

public static class TicketErrors
{
    public static DomainError InvalidTicketAssociation(InventorySeatId seatId, GeneralAdmissionPoolId poolId) => new(
        "Ticketing.Tickets.InvalidTicketAssociation",
        $"A ticket cannot be associated with both an inventory seat (ID: '{seatId.Value}') and a general admission pool (ID: '{poolId.Value}').");

    public static DomainError MissingTicketAssociation() => new(
        "Ticketing.Tickets.MissingTicketAssociation",
        "A ticket must be associated with either an inventory seat or a general admission pool.");

    public static DomainError OrderMustBeCompleted(OrderId orderId) => new(
        "Ticketing.Tickets.OrderMustBeCompleted",
        $"Tickets can only be issued for completed order '{orderId.Value}'.");

    public static DomainError GeneratedTicketCodeWasEmpty() => new(
        "Ticketing.Tickets.GeneratedTicketCodeWasEmpty",
        "Generated ticket code was empty.");

    public static DomainError InvalidIssuedTicketCount(int expected, int actual) => new(
        "Ticketing.Tickets.InvalidIssuedTicketCount",
        $"Expected to issue {expected} tickets, but issued {actual}.");

    public static DomainError InvalidIssuedTicketCount() => new(
        "Ticketing.Tickets.InvalidIssuedTicketCount",
        "Generated ticket codes must be unique and match the total quantity of tickets to be issued.");

    public static DomainError GeneratedDuplicateCodesInBatch() => new(
        "Ticketing.Tickets.GeneratedDuplicateCodesInBatch",
        "Generated ticket codes contain duplicates.");

    public static DomainError OrderHasNoItems(OrderId orderId) => new(
        "Ticketing.Tickets.OrderHasNoItems",
        $"Order '{orderId.Value}' has no items.");

    public static DomainError OrderHasInvalidTotalQuantity(OrderId orderId, int totalQuantity) => new(
        "Ticketing.Tickets.OrderHasInvalidTotalQuantity",
        $"Order '{orderId.Value}' has an invalid total quantity of '{totalQuantity}'.");

    public static DomainError OrderInventoryMismatch(OrderId orderId, InventoryId orderInventoryId, InventoryId inventoryId) => new(
        "Ticketing.Tickets.OrderInventoryMismatch",
        $"Order '{orderId.Value}' is associated with inventory '{orderInventoryId.Value}', but was expected to be associated with inventory '{inventoryId.Value}'.");
}