using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Domain.Tickets;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Tickets;

public sealed class TicketTests
{
    private static readonly PublishedEventReferenceId EventId = new(Guid.CreateVersion7());
    private static readonly InventoryId InventoryId = new(Guid.CreateVersion7());
    private static readonly OrderId OrderId = new(Guid.CreateVersion7());
    private static readonly OrderItemId OrderItemId = new(Guid.CreateVersion7());
    private static readonly TicketCode TicketCode = new("ABCDEFGHJKMNPQRS");
    private static readonly InventorySeatId SeatId = new(Guid.CreateVersion7());
    private static readonly GeneralAdmissionPoolId PoolId = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void CreateForInventorySeat_InitializesWithIssuedStatus()
    {
        // Act
        var ticket = Ticket.CreateForInventorySeat(
            EventId,
            OrderId,
            OrderItemId,
            TicketCode,
            InventoryId,
            SeatId,
            Now);

        // Assert
        Assert.Equal(TicketStatus.Issued, ticket.Status);
        Assert.Equal(EventId, ticket.PublishedEventReferenceId);
        Assert.Equal(OrderId, ticket.OrderId);
        Assert.Equal(OrderItemId, ticket.OrderItemId);
        Assert.Equal(TicketCode, ticket.Code);
        Assert.Equal(InventoryId, ticket.InventoryId);
        Assert.Equal(SeatId, ticket.InventorySeatId);
        Assert.Null(ticket.GeneralAdmissionPoolId);
        Assert.Equal(Now, ticket.CreatedAt);
    }

    [Fact]
    public void CreateForGeneralAdmissionPool_InitializesWithIssuedStatus()
    {
        // Act
        var ticket = Ticket.CreateForGeneralAdmissionPool(
            EventId,
            OrderId,
            OrderItemId,
            TicketCode,
            InventoryId,
            PoolId,
            Now);

        // Assert
        Assert.Equal(TicketStatus.Issued, ticket.Status);
        Assert.Equal(EventId, ticket.PublishedEventReferenceId);
        Assert.Equal(OrderId, ticket.OrderId);
        Assert.Equal(OrderItemId, ticket.OrderItemId);
        Assert.Equal(TicketCode, ticket.Code);
        Assert.Equal(InventoryId, ticket.InventoryId);
        Assert.Null(ticket.InventorySeatId);
        Assert.Equal(PoolId, ticket.GeneralAdmissionPoolId);
        Assert.Equal(Now, ticket.CreatedAt);
    }

    [Fact]
    public void Cancel_WhenIssuedTicket_TransitionsToCanceledAndReturnsTrue()
    {
        // Arrange
        var ticket = Ticket.CreateForInventorySeat(
            EventId,
            OrderId,
            OrderItemId,
            TicketCode,
            InventoryId,
            SeatId,
            Now);

        // Act
        var result = ticket.Cancel(Now);

        // Assert
        Assert.True(result);
        Assert.Equal(TicketStatus.Canceled, ticket.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCanceled_ReturnsFalseAndDoesNotChange()
    {
        // Arrange
        var ticket = Ticket.CreateForInventorySeat(
            EventId,
            OrderId,
            OrderItemId,
            TicketCode,
            InventoryId,
            SeatId,
            Now);
        ticket.Cancel(Now);

        // Act
        var result = ticket.Cancel(Now);

        // Assert
        Assert.False(result);
        Assert.Equal(TicketStatus.Canceled, ticket.Status);
    }

    [Fact]
    public void Cancel_IsIdempotent_MultipleCallsPreserveStatus()
    {
        // Arrange
        var ticket = Ticket.CreateForInventorySeat(
            EventId,
            OrderId,
            OrderItemId,
            TicketCode,
            InventoryId,
            SeatId,
            Now);

        // Act
        var firstResult = ticket.Cancel(Now);
        var secondResult = ticket.Cancel(Now);
        var thirdResult = ticket.Cancel(Now);

        // Assert
        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.False(thirdResult);
        Assert.Equal(TicketStatus.Canceled, ticket.Status);
    }

    [Fact]
    public void Cancel_WhenCanceledAtIsDefault_ThrowsArgumentException()
    {
        // Arrange
        var ticket = Ticket.CreateForInventorySeat(
            EventId,
            OrderId,
            OrderItemId,
            TicketCode,
            InventoryId,
            SeatId,
            Now);

        // Act
        var ex = Assert.Throws<ArgumentException>(() => ticket.Cancel(default));

        // Assert
        Assert.Contains("Cancellation time cannot be the default value", ex.Message);
    }
}
