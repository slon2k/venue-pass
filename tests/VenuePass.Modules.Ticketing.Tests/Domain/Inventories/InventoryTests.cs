using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Inventories;

public sealed class InventoryTests
{
    [Fact]
    public void CreateFromManifest_WhenManifestContainsSeats_CreatesOneInventorySeatPerSourceSeat()
    {
        // Arrange
        var eventReferenceId = PublishedEventReferenceId.Create();
        var seatIds = new[] { Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7() };
        var manifest = CreateManifest(
            sections:
            [
                new InventorySectionInput(
                    "Main",
                    [
                        new InventoryRowInput("A", [new InventorySeatInput(seatIds[0], "1"), new InventorySeatInput(seatIds[1], "2")]),
                        new InventoryRowInput("B", [new InventorySeatInput(seatIds[2], "1")])
                    ])
            ]);

        // Act
        var inventory = Inventory.CreateFromManifest(eventReferenceId, manifest);

        // Assert
        Assert.Equal(3, inventory.Seats.Count);
        Assert.All(seatIds, seatId => Assert.Contains(inventory.Seats, seat => seat.SourceSeatId == seatId));
    }

    [Fact]
    public void CreateFromManifest_WhenManifestContainsSeats_CopiesSeatMetadata()
    {
        // Arrange
        var eventReferenceId = PublishedEventReferenceId.Create();
        var sourceSeatId = Guid.CreateVersion7();
        var manifest = CreateManifest(
            sections:
            [
                new InventorySectionInput(
                    "Balcony",
                    [new InventoryRowInput("C", [new InventorySeatInput(sourceSeatId, "14")])])
            ]);

        // Act
        var inventory = Inventory.CreateFromManifest(eventReferenceId, manifest);

        // Assert
        var seat = Assert.Single(inventory.Seats);
        Assert.Equal(sourceSeatId, seat.SourceSeatId);
        Assert.Equal("Balcony", seat.Section.Value);
        Assert.Equal("C", seat.Row.Value);
        Assert.Equal("14", seat.Seat.Value);
    }

    [Fact]
    public void CreateFromManifest_WhenManifestContainsSeats_InitializesSeatsAsAvailable()
    {
        // Arrange
        var eventReferenceId = PublishedEventReferenceId.Create();
        var manifest = CreateManifest(
            sections:
            [
                new InventorySectionInput(
                    "Main",
                    [new InventoryRowInput("A", [new InventorySeatInput(Guid.CreateVersion7(), "1")])])
            ]);

        // Act
        var inventory = Inventory.CreateFromManifest(eventReferenceId, manifest);

        // Assert
        var seat = Assert.Single(inventory.Seats);
        Assert.Equal(SeatAvailability.Available, seat.Availability);
    }

    [Fact]
    public void CreateFromManifest_WhenManifestContainsGeneralAdmissionAreas_CreatesPoolsWithFullAvailability()
    {
        // Arrange
        var eventReferenceId = PublishedEventReferenceId.Create();
        var areaId = Guid.CreateVersion7();
        var manifest = CreateManifest(
            generalAdmissionAreas:
            [
                new InventoryGeneralAdmissionAreaInput(areaId, "Floor", 250),
                new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Lawn", 100)
            ]);

        // Act
        var inventory = Inventory.CreateFromManifest(eventReferenceId, manifest);

        // Assert
        Assert.Equal(2, inventory.Pools.Count);

        var floorPool = Assert.Single(inventory.Pools, pool => pool.SourceAreaId == areaId);
        Assert.Equal("Floor", floorPool.Name.Value);
        Assert.Equal(250, floorPool.Capacity.Value);
        Assert.Equal(250, floorPool.AvailableCount);
    }

    [Fact]
    public void CreateFromManifest_PreservesEventReferenceId()
    {
        // Arrange
        var eventReferenceId = PublishedEventReferenceId.Create();
        var manifest = CreateManifest(
            sections:
            [
                new InventorySectionInput(
                    "Main",
                    [new InventoryRowInput("A", [new InventorySeatInput(Guid.CreateVersion7(), "1")])])
            ],
            generalAdmissionAreas:
            [new InventoryGeneralAdmissionAreaInput(Guid.CreateVersion7(), "Floor", 50)]);

        // Act
        var inventory = Inventory.CreateFromManifest(eventReferenceId, manifest);

        // Assert
        Assert.Equal(eventReferenceId, inventory.EventReferenceId);
    }

    [Fact]
    public void CreateFromManifest_WhenManifestContainsNoSeatsOrPools_ThrowsDomainRuleViolation()
    {
        // Arrange
        var eventReferenceId = PublishedEventReferenceId.Create();
        var manifest = CreateManifest();

        // Act
        var exception = Assert.Throws<DomainRuleViolationException>(() => Inventory.CreateFromManifest(eventReferenceId, manifest));

        // Assert
        Assert.Equal(InventoryErrors.MustContainInventoryItems().Code, exception.Code);
        Assert.Equal(InventoryErrors.MustContainInventoryItems().Message, exception.Message);
    }

    private static InventoryManifest CreateManifest(
        IReadOnlyList<InventorySectionInput>? sections = null,
        IReadOnlyList<InventoryGeneralAdmissionAreaInput>? generalAdmissionAreas = null)
    {
        return new InventoryManifest(
            sections ?? [],
            generalAdmissionAreas ?? []);
    }
}