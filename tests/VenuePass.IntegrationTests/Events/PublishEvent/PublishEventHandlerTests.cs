using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.BuildingBlocks.Domain;
using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Events.Contracts.IntegrationEvents;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Features.PublishEvent;
using VenuePass.Modules.Events.Infrastructure;

using DomainEvent = VenuePass.Modules.Events.Domain.Events.Event;
using DomainEventId = VenuePass.Modules.Events.Domain.Events.EventId;
using DomainEventName = VenuePass.Modules.Events.Domain.Events.EventName;

using Xunit;

namespace VenuePass.IntegrationTests.Events.PublishEvent;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class PublishEventHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureDate = FixedNow.AddDays(30);

    private readonly EventsIntegrationTestFixture _fixture;

    public PublishEventHandlerTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_WhenEventPublished_WritesOneOutboxMessageWithCorrectPayload()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        var (eventId, manifestId, managerId) = await CreatePublishableSetupAsync(db);
        var timeProvider = new FakeTimeProvider(FixedNow);
        var handler = new PublishEventHandler(db, timeProvider);
        var command = new PublishEventCommand(eventId.Value, managerId.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var messages = await db.OutboxMessages
            .Where(m => m.Payload.Contains(manifestId.Value.ToString()))
            .ToListAsync();
        var message = Assert.Single(messages);

        Assert.Equal(typeof(EventPublishedIntegrationEvent).AssemblyQualifiedName, message.Type);
        Assert.NotNull(message.Payload);

        var payload = JsonSerializer.Deserialize<EventPublishedIntegrationEvent>(message.Payload);
        Assert.NotNull(payload);
        Assert.Equal(eventId.Value, payload!.EventId);
        Assert.Equal(manifestId, payload.ManifestId);
        Assert.NotEqual(Guid.Empty, payload.MessageId);
    }

    [Fact]
    public async Task Handle_WhenEventPublished_FreezesAssociatedManifest()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        var (eventId, manifestId, managerId) = await CreatePublishableSetupAsync(db);
        var timeProvider = new FakeTimeProvider(FixedNow);
        var handler = new PublishEventHandler(db, timeProvider);
        var command = new PublishEventCommand(eventId.Value, managerId.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var frozenManifest = await db.Manifests.FirstAsync(m => m.Id == manifestId);
        Assert.True(frozenManifest.IsFrozen);
    }

    [Fact]
    public async Task Handle_WhenManifestDoesNotExist_Fails()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        var suffix = Guid.NewGuid().ToString("N");
        var orphanManifestId = ManifestId.Create();
        var managerId = new UserId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var timeProvider = new FakeTimeProvider(FixedNow);

        var venue = Venue.Create(
            new VenueName($"Test Venue {suffix}"),
            new VenueAddress(new StreetAddress("123 Main St"), new City($"Seattle-{suffix}"), new Country("US")),
            new VenueCapacity(1000));
        var venueId = venue.Id;
        var eventId = DomainEventId.Create();

        var @event = DomainEvent.Create(
            eventId,
            venueId,
            orphanManifestId,
            new DomainEventName("No Manifest Event"),
            FutureDate,
            null,
            managerId,
            timeProvider);

        db.Venues.Add(venue);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var handler = new PublishEventHandler(db, timeProvider);
        var command = new PublishEventCommand(eventId.Value, managerId.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Events.PublishEvent.ManifestNotFound", result.Error.Code);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<(DomainEventId EventId, ManifestId ManifestId, UserId ManagerId)> CreatePublishableSetupAsync(
        EventsDbContext db)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var managerId = new UserId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var timeProvider = new FakeTimeProvider(FixedNow);

        var venue = Venue.Create(
            new VenueName($"Test Venue {suffix}"),
            new VenueAddress(new StreetAddress("123 Main St"), new City($"Seattle-{suffix}"), new Country("US")),
            new VenueCapacity(1000));
        var venueId = venue.Id;

        var template = ManifestTemplate.Create(
            new ManifestTemplateName("Layout"),
            null,
            venueId,
            [new SectionDraft("Main", [new RowDraft("A", [new SeatDraft("1")])])],
            [new GeneralAdmissionAreaDraft("Floor", 250)]);

        var eventId = DomainEventId.Create();
        var manifest = Manifest.CreateFromTemplate(eventId, template);

        var @event = DomainEvent.Create(
            eventId,
            venueId,
            manifest.Id,
            new DomainEventName("Test Event"),
            FutureDate,
            null,
            managerId,
            timeProvider);

        db.Venues.Add(venue);
        db.Events.Add(@event);
        db.Manifests.Add(manifest);
        await db.SaveChangesAsync();

        return (eventId, manifest.Id, managerId);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
