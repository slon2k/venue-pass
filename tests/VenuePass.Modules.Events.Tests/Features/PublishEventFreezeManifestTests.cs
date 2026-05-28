using Microsoft.EntityFrameworkCore;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Features.PublishEvent;
using VenuePass.Modules.Events.Infrastructure;
using Xunit;

namespace VenuePass.Modules.Events.Tests.Features;

public sealed class PublishEventFreezeManifestTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureDate = FixedNow.AddDays(30);

    [Fact]
    public async Task Handle_WhenEventPublished_FreezesAssociatedManifest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new EventsDbContext(options);

        var managerId = new UserId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var venueId = VenueId.Create();
        var venueEventId = EventId.Create();
        var timeProvider = new FakeTimeProvider(FixedNow);

        var template = ManifestTemplate.Create(
            new ManifestTemplateName("Layout"),
            null,
            venueId,
            [new SectionDraft("Main", [new RowDraft("A", [new SeatDraft("1")])])],
            []);

        var manifest = Manifest.CreateFromTemplate(venueEventId, template);

        var @event = Event.Create(
            venueEventId,
            venueId,
            manifest.Id,
            new EventName("Freeze Test Event"),
            FutureDate,
            null,
            managerId,
            timeProvider);

        db.Manifests.Add(manifest);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var handler = new PublishEventHandler(db, timeProvider);
        var command = new PublishEventCommand(venueEventId.Value, managerId.Value);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var frozenManifest = await db.Manifests.FirstAsync(m => m.Id == manifest.Id);
        Assert.True(frozenManifest.IsFrozen);
    }

    [Fact]
    public async Task Handle_WhenManifestDoesNotExist_StillSucceeds()
    {
        // Arrange — event references a manifest ID not persisted in this context
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new EventsDbContext(options);

        var managerId = new UserId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var venueEventId = EventId.Create();
        var orphanManifestId = ManifestId.Create();
        var timeProvider = new FakeTimeProvider(FixedNow);

        var @event = Event.Create(
            venueEventId,
            VenueId.Create(),
            orphanManifestId,
            new EventName("No Manifest Event"),
            FutureDate,
            null,
            managerId,
            timeProvider);

        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var handler = new PublishEventHandler(db, timeProvider);
        var command = new PublishEventCommand(venueEventId.Value, managerId.Value);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — graceful: no manifest found does not break publication
        Assert.True(result.IsSuccess);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
