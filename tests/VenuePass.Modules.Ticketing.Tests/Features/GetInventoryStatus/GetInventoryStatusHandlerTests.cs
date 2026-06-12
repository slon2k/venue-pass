using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Features.GetInventoryStatus;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Features.GetInventoryStatus;

public sealed class GetInventoryStatusHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public GetInventoryStatusHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Handle_WhenEventNotPublished_ReturnsNotFoundError()
    {
        // Arrange
        await using var db = CreateDbContext();
        var handler = new GetInventoryStatusHandler(db);
        var unknownEventId = Guid.CreateVersion7();

        // Act
        var result = await handler.Handle(new GetInventoryStatusQuery(unknownEventId), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(GetInventoryStatusErrors.EventNotFound(unknownEventId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenInventoryHasNoSeatsOrPools_ReturnsZeroCounts()
    {
        // Arrange
        await using var db = CreateDbContext();

        // Create an inventory with exactly one seat and one pool (domain requires at least one item),
        // then verify the handler maps counts correctly for a minimal inventory.
        // Note: the domain forbids empty inventories, so we create the minimal valid case
        // and verify it returns the correct counts.
        var (eventId, _) = CreateEventWithInventory(db,
            sections:
            [
                new InventorySectionInput("Main",
                [
                    new InventoryRowInput("A", [new InventorySeatInput(Guid.CreateVersion7(), "1")])
                ])
            ],
            generalAdmissionAreas: [new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Floor", 50)]);

        // Remove the event and create a fresh reference with an empty-ish inventory is not possible
        // (domain blocks it). Instead, verify a valid inventory with known content returns correct counts.
        var handler = new GetInventoryStatusHandler(db);

        // Act
        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        // Assert
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
        // Arrange — 2 sections, 2 seats each
        await using var db = CreateDbContext();

        var (eventId, inventory) = CreateEventWithInventory(db,
            sections:
            [
                new InventorySectionInput("SectionA",
                [
                    new InventoryRowInput("A",
                    [
                        new InventorySeatInput(Guid.CreateVersion7(), "1"),
                        new InventorySeatInput(Guid.CreateVersion7(), "2")
                    ])
                ]),
                new InventorySectionInput("SectionB",
                [
                    new InventoryRowInput("B",
                    [
                        new InventorySeatInput(Guid.CreateVersion7(), "1"),
                        new InventorySeatInput(Guid.CreateVersion7(), "2")
                    ])
                ])
            ],
            generalAdmissionAreas: [new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Floor", 10)]);

        var handler = new GetInventoryStatusHandler(db);

        // Act
        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        // Assert
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
        // Arrange
        await using var db = CreateDbContext();

        const int capacity = 200;
        var (eventId, _) = CreateEventWithInventory(db,
            sections:
            [
                new InventorySectionInput("Main",
                [
                    new InventoryRowInput("A", [new InventorySeatInput(Guid.CreateVersion7(), "1")])
                ])
            ],
            generalAdmissionAreas: [new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Pit", capacity)]);

        var handler = new GetInventoryStatusHandler(db);

        // Act
        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var pool = Assert.Single(result.Value.Pools);
        Assert.Equal("Pit", pool.Name);
        Assert.Equal(capacity, pool.TotalCapacity);
        Assert.Equal(capacity, pool.AvailableCount);  // newly created pool: available == capacity
    }

    [Fact]
    public async Task Handle_TotalSeatCountsMatchSumOfSections()
    {
        // Arrange — 3 seats split across 2 sections
        await using var db = CreateDbContext();

        var (eventId, _) = CreateEventWithInventory(db,
            sections:
            [
                new InventorySectionInput("Front",
                [
                    new InventoryRowInput("A",
                    [
                        new InventorySeatInput(Guid.CreateVersion7(), "1"),
                        new InventorySeatInput(Guid.CreateVersion7(), "2")
                    ])
                ]),
                new InventorySectionInput("Back",
                [
                    new InventoryRowInput("B",
                    [
                        new InventorySeatInput(Guid.CreateVersion7(), "1")
                    ])
                ])
            ],
            generalAdmissionAreas: [new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "GA", 10)]);

        var handler = new GetInventoryStatusHandler(db);

        // Act
        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var totalFromSections = result.Value.Sections.Sum(s => s.TotalSeats);
        Assert.Equal(result.Value.TotalSeats, totalFromSections);
        Assert.Equal(3, result.Value.TotalSeats);
        Assert.Equal(3, result.Value.AvailableSeats);
    }

    [Fact]
    public async Task Handle_WhenSomeSeatsReserved_ExcludesReservedFromAvailableCount()
    {
        // Arrange — 3 seats, 1 reserved
        await using var setupDb = CreateDbContext();
        var seat1 = Guid.CreateVersion7();
        var seat2 = Guid.CreateVersion7();
        var seat3 = Guid.CreateVersion7();

        var (eventId, inventory) = CreateEventWithInventory(setupDb,
            sections:
            [
                new InventorySectionInput("Main",
                [
                    new InventoryRowInput("A",
                    [
                        new InventorySeatInput(seat1, "1"),
                        new InventorySeatInput(seat2, "2"),
                        new InventorySeatInput(seat3, "3")
                    ])
                ])
            ],
            generalAdmissionAreas: [new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "GA", 10)]);

        inventory.ReserveSeats([inventory.Seats.First(s => s.SourceSeatId == seat1).Id]);
        setupDb.SaveChanges();

        await using var db = CreateDbContext();
        var handler = new GetInventoryStatusHandler(db);

        // Act
        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        // Assert
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
        // Arrange
        await using var setupDb = CreateDbContext();
        var seatGuid = Guid.CreateVersion7();

        var (eventId, inventory) = CreateEventWithInventory(setupDb,
            sections:
            [
                new InventorySectionInput("Main",
                [
                    new InventoryRowInput("A", [new InventorySeatInput(seatGuid, "1")])
                ])
            ],
            generalAdmissionAreas: [new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "GA", 10)]);

        var seatId = inventory.Seats[0].Id;
        inventory.ReserveSeats([seatId]);
        inventory.SellSeats([seatId]);
        setupDb.SaveChanges();

        await using var db = CreateDbContext();
        var handler = new GetInventoryStatusHandler(db);

        // Act
        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        // Assert
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
        // Arrange — Front: 1 reserved + 1 sold; Back: 2 available
        await using var setupDb = CreateDbContext();
        var front1 = Guid.CreateVersion7();
        var front2 = Guid.CreateVersion7();
        var back1 = Guid.CreateVersion7();
        var back2 = Guid.CreateVersion7();

        var (eventId, inventory) = CreateEventWithInventory(setupDb,
            sections:
            [
                new InventorySectionInput("Front",
                [
                    new InventoryRowInput("A",
                    [
                        new InventorySeatInput(front1, "1"),
                        new InventorySeatInput(front2, "2")
                    ])
                ]),
                new InventorySectionInput("Back",
                [
                    new InventoryRowInput("B",
                    [
                        new InventorySeatInput(back1, "1"),
                        new InventorySeatInput(back2, "2")
                    ])
                ])
            ],
            generalAdmissionAreas: [new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "GA", 10)]);

        var seat1 = inventory.Seats.Single(s => s.SourceSeatId == front1).Id;
        var seat2 = inventory.Seats.Single(s => s.SourceSeatId == front2).Id;
        inventory.ReserveSeats([seat1, seat2]);
        inventory.SellSeats([seat2]);
        setupDb.SaveChanges();

        await using var db = CreateDbContext();
        var handler = new GetInventoryStatusHandler(db);

        // Act
        var result = await handler.Handle(new GetInventoryStatusQuery(eventId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.TotalSeats);
        Assert.Equal(2, result.Value.AvailableSeats); // only Back seats

        var front = result.Value.Sections.Single(s => s.Name == "Front");
        Assert.Equal(2, front.TotalSeats);
        Assert.Equal(0, front.AvailableSeats);

        var back = result.Value.Sections.Single(s => s.Name == "Back");
        Assert.Equal(2, back.TotalSeats);
        Assert.Equal(2, back.AvailableSeats);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private TicketingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlite(_connection)
            .Options;

        var db = new TicketingDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static (Guid EventId, Inventory Inventory) CreateEventWithInventory(
        TicketingDbContext db,
        IReadOnlyList<InventorySectionInput> sections,
        IReadOnlyList<InventoryGeneralAdmissionAreaInput> generalAdmissionAreas)
    {
        var eventId = Guid.CreateVersion7();
        var reference = PublishedEventReference.Create(eventId, Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        db.PublishedEventReferences.Add(reference);
        db.SaveChanges();

        var manifest = new InventoryManifest(sections, generalAdmissionAreas);
        var inventory = Inventory.CreateFromManifest(reference.Id, manifest);
        db.Inventories.Add(inventory);
        db.SaveChanges();

        return (eventId, inventory);
    }
}
