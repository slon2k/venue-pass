using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Domain.Tickets;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Tickets;

public sealed class TicketTests
{
    private static readonly PublishedEventReferenceId EventId = new(Guid.CreateVersion7());
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
            SeatId,
            Now);

        // Assert
        Assert.Equal(TicketStatus.Issued, ticket.Status);
        Assert.Equal(EventId, ticket.PublishedEventReferenceId);
        Assert.Equal(OrderId, ticket.OrderId);
        Assert.Equal(OrderItemId, ticket.OrderItemId);
        Assert.Equal(TicketCode, ticket.Code);
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
            PoolId,
            Now);

        // Assert
        Assert.Equal(TicketStatus.Issued, ticket.Status);
        Assert.Equal(EventId, ticket.PublishedEventReferenceId);
        Assert.Equal(OrderId, ticket.OrderId);
        Assert.Equal(OrderItemId, ticket.OrderItemId);
        Assert.Equal(TicketCode, ticket.Code);
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
            SeatId,
            Now);

        // Act
        var result = ticket.Cancel();

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
            SeatId,
            Now);
        ticket.Cancel();

        // Act
        var result = ticket.Cancel();

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
            SeatId,
            Now);

        // Act
        var firstResult = ticket.Cancel();
        var secondResult = ticket.Cancel();
        var thirdResult = ticket.Cancel();

        // Assert
        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.False(thirdResult);
        Assert.Equal(TicketStatus.Canceled, ticket.Status);
    }
}
