using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using VenuePass.Modules.Ticketing.Contracts;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Domain.Tickets;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Infrastructure;

public sealed class TicketModuleContractTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public TicketModuleContractTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task ValidateTicketForEvent_WhenEventDoesNotExist_ReturnsEventNotFound()
    {
        await using var db = CreateDbContext();
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForEventAsync(
            "ABCDEFGHJKMNPQRS",
            Guid.CreateVersion7());

        Assert.False(result.IsValid);
        Assert.True(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.IncorrectEvent, result.FailureReason);
    }

    [Fact]
    public async Task ValidateTicketForEvent_WhenTicketCodeIsMalformed_ReturnsMalformedTicketCode()
    {
        await using var db = CreateDbContext();
        var (reference, _) = CreateEventAndSave(db);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForEventAsync(
            "NOT-VALID!",
            reference.EventId);

        Assert.False(result.IsValid);
        Assert.False(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.MalformedTicketCode, result.FailureReason);
    }

    [Fact]
    public async Task ValidateTicketForEvent_WhenTicketDoesNotExist_ReturnsTicketNotFound()
    {
        await using var db = CreateDbContext();
        var (reference, _) = CreateEventAndSave(db);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForEventAsync(
            "ABCDEFGHJKMNPQRS",
            reference.EventId);

        Assert.False(result.IsValid);
        Assert.False(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.TicketNotFound, result.FailureReason);
    }

    [Fact]
    public async Task ValidateTicketForEvent_WhenIssuedTicket_ReturnsValid()
    {
        await using var db = CreateDbContext();
        var (reference, seatId) = CreateEventAndSave(db);
        var ticket = CreateAndSaveTicket(db, reference.Id, seatId, TicketStatus.Issued);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForEventAsync(
            ticket.Code.Value,
            reference.EventId);

        Assert.True(result.IsValid);
        Assert.True(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.None, result.FailureReason);
        Assert.NotNull(result.Ticket);
        Assert.Equal(ticket.Id.Value, result.Ticket.TicketId);
        Assert.Equal(ticket.Code.Value, result.Ticket.TicketCode);
        Assert.Equal(TicketValidationStatus.Issued, result.Ticket.Status);
        Assert.Equal(reference.EventId, result.Ticket.PublishedEventReferenceId);
    }

    [Fact]
    public async Task ValidateTicketForEvent_WhenCanceledTicket_ReturnsInvalidWithTicketCanceledReason()
    {
        await using var db = CreateDbContext();
        var (reference, seatId) = CreateEventAndSave(db);
        var ticket = CreateAndSaveTicket(db, reference.Id, seatId, TicketStatus.Canceled);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForEventAsync(
            ticket.Code.Value,
            reference.EventId);

        Assert.False(result.IsValid);
        Assert.True(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.TicketCanceled, result.FailureReason);
        Assert.NotNull(result.Ticket);
        Assert.Equal(TicketValidationStatus.Canceled, result.Ticket.Status);
    }

    [Fact]
    public async Task ValidateTicketForEvent_WhenTicketBelongsToDifferentEvent_ReturnsIncorrectEvent()
    {
        await using var db = CreateDbContext();
        var (reference, seatId) = CreateEventAndSave(db);
        var (otherReference, _) = CreateEventAndSave(db);
        var ticket = CreateAndSaveTicket(db, reference.Id, seatId, TicketStatus.Issued);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForEventAsync(
            ticket.Code.Value,
            otherReference.EventId);

        Assert.False(result.IsValid);
        Assert.True(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.IncorrectEvent, result.FailureReason);
    }

    [Fact]
    public async Task ValidateTicketForEvent_WhenIssuedTicket_TicketDtoContainsAllRequiredIdentifiers()
    {
        await using var db = CreateDbContext();
        var (reference, seatId) = CreateEventAndSave(db);
        var ticket = CreateAndSaveTicket(db, reference.Id, seatId, TicketStatus.Issued);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForEventAsync(
            ticket.Code.Value,
            reference.EventId);

        Assert.True(result.IsValid);
        var dto = result.Ticket!;
        Assert.Equal(ticket.Id.Value, dto.TicketId);
        Assert.Equal(ticket.Code.Value, dto.TicketCode);
        Assert.Equal(ticket.OrderId.Value, dto.OrderId);
        Assert.Equal(ticket.OrderItemId.Value, dto.OrderItemId);
        Assert.Equal(reference.EventId, dto.PublishedEventReferenceId);
        Assert.Equal(seatId.Value, dto.InventorySeatId);
        Assert.Null(dto.GeneralAdmissionPoolId);
        Assert.Equal(TicketType.ReservedSeating, dto.TicketType);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private TicketingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlite(_connection)
            .Options;

        var db = new TicketingDbContext(options);
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
        return db;
    }

    private static (PublishedEventReference reference, InventorySeatId seatId) CreateEventAndSave(TicketingDbContext db)
    {
        var reference = PublishedEventReference.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow);

        db.PublishedEventReferences.Add(reference);
        db.SaveChanges();

        var seatId = new InventorySeatId(Guid.CreateVersion7());
        return (reference, seatId);
    }

    private static Ticket CreateAndSaveTicket(
        TicketingDbContext db,
        PublishedEventReferenceId publishedEventReferenceId,
        InventorySeatId seatId,
        TicketStatus status)
    {
        var ticket = Ticket.CreateForInventorySeat(
            publishedEventReferenceId,
            new OrderId(Guid.CreateVersion7()),
            new OrderItemId(Guid.CreateVersion7()),
            new TicketCode("ABCDEFGHJKMNPQRS"),
            seatId,
            DateTimeOffset.UtcNow);

        if (status == TicketStatus.Canceled)
        {
            ticket.Cancel();
        }

        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket;
    }
}
