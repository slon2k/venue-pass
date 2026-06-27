using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Features.GetInventoryStatus;
using VenuePass.Modules.Ticketing.Infrastructure;
using InventoryDomain = VenuePass.Modules.Ticketing.Domain.Inventories;

using Xunit;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;

namespace VenuePass.IntegrationTests.Ticketing.Inventory;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class GetInventoryStatusHandlerTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public GetInventoryStatusHandlerTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_WhenEventNotPublished_ReturnsNotFoundError()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var handler = new GetInventoryStatusHandler(db);
        var unknownEventId = Guid.CreateVersion7();

        var result = await handler.Handle(new GetInventoryStatusQuery(unknownEventId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(GetInventoryStatusErrors.EventNotFound(unknownEventId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenInventoryHasNoSeatsOrPools_ReturnsZeroCounts()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var (eventId, _) = CreateEventWithInventory(db,
            sections:
            [
                new InventoryDomain.InventorySectionInput("Main",
                [
                    new InventoryDomain.InventoryRowInput("A", [new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "1")])
                ])
            ],
            generalAdmissionAreas: [new InventoryDomain.InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Floor", 50)]);

        var handler = new GetInventoryStatusHandler(db);

        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(eventId, result.Value.EventId);
        Assert.Equal(1, result.Value.TotalSeats);
        Assert.Equal(1, result.Value.AvailableSeats);
        Assert.Single(result.Value.Sections);
        Assert.Single(result.Value.Pools);
    }

    [Fact]
    public async Task Handle_WhenInventoryHasSeats_GroupsBySectionCorrectly()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var (eventId, inventory) = CreateEventWithInventory(db,
            sections:
            [
                new InventoryDomain.InventorySectionInput("SectionA",
                [
                    new InventoryDomain.InventoryRowInput("A",
                    [
                        new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "1"),
                        new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "2")
                    ])
                ]),
                new InventoryDomain.InventorySectionInput("SectionB",
                [
                    new InventoryDomain.InventoryRowInput("B",
                    [
                        new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "1"),
                        new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "2")
                    ])
                ])
            ],
            generalAdmissionAreas: [new InventoryDomain.InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Floor", 10)]);

        var handler = new GetInventoryStatusHandler(db);

        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Sections.Count);

        var sectionA = result.Value.Sections.Single(s => s.Name == "SectionA");
        Assert.Equal(2, sectionA.TotalSeats);
        Assert.Equal(2, sectionA.AvailableSeats);

        var sectionB = result.Value.Sections.Single(s => s.Name == "SectionB");
        Assert.Equal(2, sectionB.TotalSeats);
        Assert.Equal(2, sectionB.AvailableSeats);
    }

    [Fact]
    public async Task Handle_WhenInventoryHasPools_ReturnsPoolStatus()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        const int capacity = 200;
        var (eventId, _) = CreateEventWithInventory(db,
            sections:
            [
                new InventoryDomain.InventorySectionInput("Main",
                [
                    new InventoryDomain.InventoryRowInput("A", [new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "1")])
                ])
            ],
            generalAdmissionAreas: [new InventoryDomain.InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Pit", capacity)]);

        var handler = new GetInventoryStatusHandler(db);

        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var pool = Assert.Single(result.Value.Pools);
        Assert.Equal("Pit", pool.Name);
        Assert.Equal(capacity, pool.TotalCapacity);
        Assert.Equal(capacity, pool.AvailableCount);
    }

    [Fact]
    public async Task Handle_TotalSeatCountsMatchSumOfSections()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var (eventId, _) = CreateEventWithInventory(db,
            sections:
            [
                new InventoryDomain.InventorySectionInput("Front",
                [
                    new InventoryDomain.InventoryRowInput("A",
                    [
                        new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "1"),
                        new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "2")
                    ])
                ]),
                new InventoryDomain.InventorySectionInput("Back",
                [
                    new InventoryDomain.InventoryRowInput("B",
                    [
                        new InventoryDomain.InventorySeatInput(Guid.CreateVersion7(), "1")
                    ])
                ])
            ],
            generalAdmissionAreas: [new InventoryDomain.InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "GA", 10)]);

        var handler = new GetInventoryStatusHandler(db);

        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var totalFromSections = result.Value.Sections.Sum(s => s.TotalSeats);
        Assert.Equal(result.Value.TotalSeats, totalFromSections);
        Assert.Equal(3, result.Value.TotalSeats);
        Assert.Equal(3, result.Value.AvailableSeats);
    }

    [Fact]
    public async Task Handle_WhenSomeSeatsReserved_ExcludesReservedFromAvailableCount()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var seat1 = Guid.CreateVersion7();
        var seat2 = Guid.CreateVersion7();
        var seat3 = Guid.CreateVersion7();

        var (eventId, inventory) = CreateEventWithInventory(db,
            sections:
            [
                new InventoryDomain.InventorySectionInput("Main",
                [
                    new InventoryDomain.InventoryRowInput("A",
                    [
                        new InventoryDomain.InventorySeatInput(seat1, "1"),
                        new InventoryDomain.InventorySeatInput(seat2, "2"),
                        new InventoryDomain.InventorySeatInput(seat3, "3")
                    ])
                ])
            ],
            generalAdmissionAreas: [new InventoryDomain.InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "GA", 10)]);

        inventory.ReserveSeats([inventory.Seats.First(s => s.SourceSeatId == seat1).Id]);
        db.SaveChanges();

        var handler = new GetInventoryStatusHandler(db);

        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalSeats);
        Assert.Equal(2, result.Value.AvailableSeats);

        var section = Assert.Single(result.Value.Sections);
        Assert.Equal(3, section.TotalSeats);
        Assert.Equal(2, section.AvailableSeats);
    }

    [Fact]
    public async Task Handle_WhenAllSeatsSold_ReturnsZeroAvailableSeats()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var seatGuid = Guid.CreateVersion7();

        var (eventId, inventory) = CreateEventWithInventory(db,
            sections:
            [
                new InventoryDomain.InventorySectionInput("Main",
                [
                    new InventoryDomain.InventoryRowInput("A", [new InventoryDomain.InventorySeatInput(seatGuid, "1")])
                ])
            ],
            generalAdmissionAreas: [new InventoryDomain.InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "GA", 10)]);

        var seatId = inventory.Seats[0].Id;
        inventory.ReserveSeats([seatId]);
        inventory.SellSeats([seatId]);
        db.SaveChanges();

        var handler = new GetInventoryStatusHandler(db);

        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalSeats);
        Assert.Equal(0, result.Value.AvailableSeats);

        var section = Assert.Single(result.Value.Sections);
        Assert.Equal(1, section.TotalSeats);
        Assert.Equal(0, section.AvailableSeats);
    }

    [Fact]
    public async Task Handle_WithMixedSeatStates_ReturnsCorrectCountsPerSection()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var front1 = Guid.CreateVersion7();
        var front2 = Guid.CreateVersion7();
        var back1 = Guid.CreateVersion7();
        var back2 = Guid.CreateVersion7();

        var (eventId, inventory) = CreateEventWithInventory(db,
            sections:
            [
                new InventoryDomain.InventorySectionInput("Front",
                [
                    new InventoryDomain.InventoryRowInput("A",
                    [
                        new InventoryDomain.InventorySeatInput(front1, "1"),
                        new InventoryDomain.InventorySeatInput(front2, "2")
                    ])
                ]),
                new InventoryDomain.InventorySectionInput("Back",
                [
                    new InventoryDomain.InventoryRowInput("B",
                    [
                        new InventoryDomain.InventorySeatInput(back1, "1"),
                        new InventoryDomain.InventorySeatInput(back2, "2")
                    ])
                ])
            ],
            generalAdmissionAreas: [new InventoryDomain.InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "GA", 10)]);

        var seat1 = inventory.Seats.Single(s => s.SourceSeatId == front1).Id;
        var seat2 = inventory.Seats.Single(s => s.SourceSeatId == front2).Id;
        inventory.ReserveSeats([seat1, seat2]);
        inventory.SellSeats([seat2]);
        db.SaveChanges();

        var handler = new GetInventoryStatusHandler(db);

        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.TotalSeats);
        Assert.Equal(2, result.Value.AvailableSeats);

        var front = result.Value.Sections.Single(s => s.Name == "Front");
        Assert.Equal(2, front.TotalSeats);
        Assert.Equal(0, front.AvailableSeats);

        var back = result.Value.Sections.Single(s => s.Name == "Back");
        Assert.Equal(2, back.TotalSeats);
        Assert.Equal(2, back.AvailableSeats);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (Guid EventId, InventoryDomain.Inventory Inventory) CreateEventWithInventory(
        TicketingDbContext db,
        IReadOnlyList<InventoryDomain.InventorySectionInput> sections,
        IReadOnlyList<InventoryDomain.InventoryGeneralAdmissionAreaInput> generalAdmissionAreas)
    {
        var eventId = Guid.CreateVersion7();
        var reference = Modules.Ticketing.Domain.PublishedEvents.PublishedEventReference.Create(eventId, Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        db.PublishedEventReferences.Add(reference);
        db.SaveChanges();

        var manifest = new InventoryDomain.InventoryManifest(sections, generalAdmissionAreas);
        var inventory = InventoryDomain.Inventory.CreateFromManifest(reference.Id, manifest);
        db.Inventories.Add(inventory);
        db.SaveChanges();

        return (eventId, inventory);
    }
}



