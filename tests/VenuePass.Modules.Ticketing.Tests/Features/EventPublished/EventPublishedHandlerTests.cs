using Microsoft.EntityFrameworkCore;

using VenuePass.Modules.Events.Contracts;
using VenuePass.Modules.Events.Contracts.IntegrationEvents;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Features.EventPublished;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Features.EventPublished;

public sealed class EventPublishedHandlerTests
{
    [Fact]
    public async Task Handle_WhenEventAlreadySynced_DoesNothing()
    {
        // Arrange
        await using var db = CreateDbContext();
        var integrationEvent = CreateIntegrationEvent();

        db.PublishedEventReferences.Add(PublishedEventReference.Create(
            integrationEvent.EventId,
            integrationEvent.ManifestId,
            DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var contract = new RecordingEventsModuleContract(CreateManifestExport());
        var handler = new EventPublishedHandler(db, contract, new FixedTimeProvider(DateTimeOffset.UtcNow));

        // Act
        await handler.Handle(integrationEvent, CancellationToken.None);

        // Assert
        Assert.Equal(0, contract.CallCount);
        Assert.Single(db.PublishedEventReferences);
        Assert.Empty(db.Inventories);
    }

    [Fact]
    public async Task Handle_WhenManifestNotAvailable_Throws()
    {
        // Arrange
        await using var db = CreateDbContext();
        var integrationEvent = CreateIntegrationEvent();

        var contract = new RecordingEventsModuleContract(null);
        var handler = new EventPublishedHandler(db, contract, new FixedTimeProvider(DateTimeOffset.UtcNow));

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(integrationEvent, CancellationToken.None));

        // Assert
        Assert.Equal(1, contract.CallCount);
        Assert.Contains(integrationEvent.ManifestId.ToString(), exception.Message);
        Assert.Empty(db.PublishedEventReferences);
        Assert.Empty(db.Inventories);
    }

    [Fact]
    public async Task Handle_WhenManifestAvailable_CreatesReferenceAndInventory()
    {
        // Arrange
        await using var db = CreateDbContext();
        var integrationEvent = CreateIntegrationEvent();
        var now = new DateTimeOffset(2026, 6, 6, 10, 30, 0, TimeSpan.Zero);

        var contract = new RecordingEventsModuleContract(CreateManifestExport());
        var handler = new EventPublishedHandler(db, contract, new FixedTimeProvider(now));

        // Act
        await handler.Handle(integrationEvent, CancellationToken.None);

        // Assert
        Assert.Equal(1, contract.CallCount);

        var reference = Assert.Single(db.PublishedEventReferences);
        Assert.Equal(integrationEvent.EventId, reference.EventId);
        Assert.Equal(integrationEvent.ManifestId, reference.ManifestId);
        Assert.Equal(now, reference.SyncedAt);

        var inventory = Assert.Single(db.Inventories.Include(i => i.Seats).Include(i => i.Pools));
        Assert.Equal(reference.Id, inventory.EventReferenceId);
        Assert.Single(inventory.Seats);
        Assert.Single(inventory.Pools);
    }

    [Fact]
    public void IsDuplicatePublishedEventReference_WhenIndexNameAppearsInMessage_ReturnsTrue()
    {
        // Arrange
        var exception = new DbUpdateException(
            "Save failed.",
            new InvalidOperationException("Violation of UNIQUE KEY constraint 'IX_published_event_references_event_id'."));

        // Act
        bool isDuplicate = EventPublishedHandler.IsDuplicatePublishedEventReference(exception);

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public void IsDuplicatePublishedEventReference_WhenNoDuplicateSignal_ReturnsFalse()
    {
        // Arrange
        var exception = new DbUpdateException("Save failed.", new InvalidOperationException("Some other DB error."));

        // Act
        bool isDuplicate = EventPublishedHandler.IsDuplicatePublishedEventReference(exception);

        // Assert
        Assert.False(isDuplicate);
    }

    private static TicketingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TicketingDbContext(options);
    }

    private static EventPublishedIntegrationEvent CreateIntegrationEvent()
    {
        return new EventPublishedIntegrationEvent(
            MessageId: Guid.CreateVersion7(),
            EventId: Guid.CreateVersion7(),
            ManifestId: Guid.CreateVersion7(),
            OccurredOn: new DateTimeOffset(2026, 6, 6, 9, 0, 0, TimeSpan.Zero));
    }

    private static ManifestExportDto CreateManifestExport()
    {
        return new ManifestExportDto(
            ManifestId: Guid.CreateVersion7(),
            EventId: Guid.CreateVersion7(),
            Sections:
            [
                new SectionExportDto(
                    SectionId: Guid.CreateVersion7(),
                    Name: "Main",
                    Rows:
                    [
                        new RowExportDto(
                            RowId: Guid.CreateVersion7(),
                            Label: "A",
                            Seats: [new SeatExportDto(Guid.CreateVersion7(), "1")])
                    ])
            ],
            GeneralAdmissionAreas:
            [new GeneralAdmissionAreaExportDto(Guid.CreateVersion7(), "Floor", 250)]);
    }

    private sealed class RecordingEventsModuleContract(ManifestExportDto? manifest) : IEventsModuleContract
    {
        public int CallCount { get; private set; }

        public Task<ManifestExportDto?> GetManifestForTicketingAsync(Guid manifestId, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(manifest);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}