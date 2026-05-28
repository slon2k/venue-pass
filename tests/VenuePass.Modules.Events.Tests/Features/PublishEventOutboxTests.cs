using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Contracts;
using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Features.PublishEvent;
using VenuePass.Modules.Events.Infrastructure;
using Xunit;

namespace VenuePass.Modules.Events.Tests.Features;

public sealed class PublishEventOutboxTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureDate = FixedNow.AddDays(30);

    [Fact]
    public async Task Handle_WhenEventPublished_WritesOneOutboxMessageWithCorrectPayload()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new EventsDbContext(options);

        var managerId = new UserId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var venueEventId = EventId.Create();
        var manifestId = ManifestId.Create();

        var timeProvider = new FakeTimeProvider(FixedNow);

        var @event = Event.Create(
            venueEventId,
            VenueId.Create(),
            manifestId,
            new EventName("Outbox Test Event"),
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

        // Assert
        Assert.True(result.IsSuccess);

        var messages = await db.OutboxMessages.ToListAsync();
        var message = Assert.Single(messages);

        Assert.Equal(typeof(EventPublishedIntegrationEvent).FullName, message.Type);
        Assert.NotNull(message.Payload);

        var payload = JsonSerializer.Deserialize<EventPublishedIntegrationEvent>(message.Payload);
        Assert.NotNull(payload);
        Assert.Equal(venueEventId.Value, payload.VenueEventId);
        Assert.Equal(manifestId.Value, payload.ManifestId);
        Assert.NotEqual(Guid.Empty, payload.EventId);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
