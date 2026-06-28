using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Contracts;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Domain.Reservations;
using VenuePass.Modules.Ticketing.Domain.Tickets;
using VenuePass.Modules.Ticketing.Infrastructure;

using InventoryDomain = VenuePass.Modules.Ticketing.Domain.Inventories;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.Contracts;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class TicketModuleContractTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public TicketModuleContractTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ValidateTicketForPublishedEventReference_WhenReferenceDoesNotExist_ReturnsPublishedEventReferenceNotFound()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForPublishedEventReferenceAsync(
            "ABCDEFGHJKMNPQRS",
            Guid.CreateVersion7());

        Assert.False(result.IsValid);
        Assert.False(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.PublishedEventReferenceNotFound, result.FailureReason);
    }

    [Fact]
    public async Task ValidateTicketForPublishedEventReference_WhenTicketCodeIsMalformed_ReturnsMalformedTicketCode()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var (reference, _, _) = CreateEventAndSave(db);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForPublishedEventReferenceAsync(
            "NOT-VALID!",
            reference.Id.Value);

        Assert.False(result.IsValid);
        Assert.False(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.MalformedTicketCode, result.FailureReason);
    }

    [Fact]
    public async Task ValidateTicketForPublishedEventReference_WhenTicketDoesNotExist_ReturnsTicketNotFound()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var (reference, _, _) = CreateEventAndSave(db);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForPublishedEventReferenceAsync(
            "ABCDEFGHJKMNPQRS",
            reference.Id.Value);

        Assert.False(result.IsValid);
        Assert.False(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.TicketNotFound, result.FailureReason);
    }

    [Fact]
    public async Task ValidateTicketForPublishedEventReference_WhenIssuedTicket_ReturnsValid()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var (reference, seatId, inventoryId) = CreateEventAndSave(db);
        var ticket = CreateAndSaveTicket(db, reference.Id, inventoryId, seatId, TicketStatus.Issued);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForPublishedEventReferenceAsync(
            ticket.Code.Value,
            reference.Id.Value);

        Assert.True(result.IsValid);
        Assert.True(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.None, result.FailureReason);
        Assert.NotNull(result.Ticket);
        Assert.Equal(ticket.Id.Value, result.Ticket.TicketId);
        Assert.Equal(ticket.Code.Value, result.Ticket.TicketCode);
        Assert.Equal(TicketValidationStatus.Issued, result.Ticket.Status);
        Assert.Equal(reference.Id.Value, result.Ticket.PublishedEventReferenceId);
    }

    [Fact]
    public async Task ValidateTicketForPublishedEventReference_WhenCanceledTicket_ReturnsInvalidWithTicketCanceledReason()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var (reference, seatId, inventoryId) = CreateEventAndSave(db);
        var ticket = CreateAndSaveTicket(db, reference.Id, inventoryId, seatId, TicketStatus.Canceled);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForPublishedEventReferenceAsync(
            ticket.Code.Value,
            reference.Id.Value);

        Assert.False(result.IsValid);
        Assert.True(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.TicketCanceled, result.FailureReason);
        Assert.NotNull(result.Ticket);
        Assert.Equal(TicketValidationStatus.Canceled, result.Ticket.Status);
    }

    [Fact]
    public async Task ValidateTicketForPublishedEventReference_WhenTicketBelongsToDifferentEvent_ReturnsIncorrectEvent()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var (reference, seatId, inventoryId) = CreateEventAndSave(db);
        var (otherReference, _, _) = CreateEventAndSave(db);
        var ticket = CreateAndSaveTicket(db, reference.Id, inventoryId, seatId, TicketStatus.Issued);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForPublishedEventReferenceAsync(
            ticket.Code.Value,
            otherReference.Id.Value);

        Assert.False(result.IsValid);
        Assert.True(result.IsFound);
        Assert.Equal(TicketValidationFailureReason.IncorrectEvent, result.FailureReason);
    }

    [Fact]
    public async Task ValidateTicketForPublishedEventReference_WhenIssuedTicket_TicketDtoContainsAllRequiredIdentifiers()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var (reference, seatId, inventoryId) = CreateEventAndSave(db);
        var ticket = CreateAndSaveTicket(db, reference.Id, inventoryId, seatId, TicketStatus.Issued);
        var contract = new TicketModuleContract(db);

        var result = await contract.ValidateTicketForPublishedEventReferenceAsync(
            ticket.Code.Value,
            reference.Id.Value);

        Assert.True(result.IsValid);
        var dto = result.Ticket!;
        Assert.Equal(ticket.Id.Value, dto.TicketId);
        Assert.Equal(ticket.Code.Value, dto.TicketCode);
        Assert.Equal(ticket.OrderId.Value, dto.OrderId);
        Assert.Equal(ticket.OrderItemId.Value, dto.OrderItemId);
        Assert.Equal(reference.Id.Value, dto.PublishedEventReferenceId);
        Assert.Equal(seatId.Value, dto.InventorySeatId);
        Assert.Null(dto.GeneralAdmissionPoolId);
        Assert.Equal(TicketType.ReservedSeating, dto.TicketType);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (PublishedEventReference reference, InventoryDomain.InventorySeatId seatId, InventoryDomain.InventoryId inventoryId) CreateEventAndSave(TicketingDbContext db)
    {
        var reference = PublishedEventReference.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow);

        db.PublishedEventReferences.Add(reference);
        db.SaveChanges();

        var sourceSeatId = Guid.CreateVersion7();
        var manifest = new InventoryDomain.InventoryManifest(
        [
            new InventoryDomain.InventorySectionInput("Main",
            [
                new InventoryDomain.InventoryRowInput("A", [new InventoryDomain.InventorySeatInput(sourceSeatId, "1")])
            ])
        ],
        []);

        var inventory = InventoryDomain.Inventory.CreateFromManifest(reference.Id, manifest);
        db.Inventories.Add(inventory);
        db.SaveChanges();

        var seatId = inventory.Seats.Single(s => s.SourceSeatId == sourceSeatId).Id;

        return (reference, seatId, inventory.Id);
    }

    private static Ticket CreateAndSaveTicket(
        TicketingDbContext db,
        PublishedEventReferenceId publishedEventReferenceId,
        InventoryDomain.InventoryId inventoryId,
        InventoryDomain.InventorySeatId seatId,
        TicketStatus status)
    {
        var now = DateTimeOffset.UtcNow;

        var inventory = db.Inventories
            .Include(i => i.Seats)
            .Single(i => i.Id == inventoryId);

        var offer = Offer.Create(
            inventoryId,
            new OfferName($"Offer-{Guid.CreateVersion7():N}"),
            DateTimeRange.Between(now.AddHours(-1), now.AddHours(1)),
            new Currency("USD"));

        offer.ConfigurePriceZone(
            inventory,
            new PriceZoneName("Main"),
            new Amount(100),
            [new PriceZoneInventorySeatItemInput(seatId)],
            []);
        offer.Activate();
        db.Offers.Add(offer);
        db.SaveChanges();

        var reservation = Reservation.Create(
            offer,
            [new ReservationItemInventorySeatInput(seatId)],
            [],
            now,
            now.AddMinutes(30));
        db.Reservations.Add(reservation);
        db.SaveChanges();

        var order = Order.CreateFromReservation(reservation, "Test Buyer", "buyer@example.com", now);
        db.Orders.Add(order);
        db.SaveChanges();

        var ticketCode = new TicketCode(Guid.CreateVersion7().ToString("N")[..16].ToUpperInvariant());

        var ticket = Ticket.CreateForInventorySeat(
            publishedEventReferenceId,
            order.Id,
            order.Items[0].Id,
            ticketCode,
            seatId,
            now);

        if (status == TicketStatus.Canceled)
        {
            ticket.Cancel(now);
        }

        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket;
    }
}
