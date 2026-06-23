using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Events.Contracts;
using VenuePass.Modules.Events.Contracts.IntegrationEvents;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Features.EventPublished;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.EventSync;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class EventPublishedHandlerTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public EventPublishedHandlerTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_WhenEventAlreadySynced_DoesNothing()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var integrationEvent = CreateIntegrationEvent();

        db.PublishedEventReferences.Add(PublishedEventReference.Create(
            integrationEvent.EventId,
            integrationEvent.ManifestId,
            DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var contract = new RecordingEventsModuleContract(CreateManifestExport());
        var handler = new EventPublishedHandler(db, contract, new FixedTimeProvider(DateTimeOffset.UtcNow));

        await handler.Handle(integrationEvent, CancellationToken.None);

        Assert.Equal(0, contract.CallCount);
        Assert.Single(await db.PublishedEventReferences
            .Where(r => r.EventId == integrationEvent.EventId).ToListAsync());
        Assert.Empty(await db.Inventories
            .Where(i => db.PublishedEventReferences
                .Where(r => r.EventId == integrationEvent.EventId)
                .Select(r => r.Id)
                .Contains(i.EventReferenceId))
            .ToListAsync());
    }

    [Fact]
    public async Task Handle_WhenManifestNotAvailable_Throws()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var integrationEvent = CreateIntegrationEvent();

        var contract = new RecordingEventsModuleContract(null);
        var handler = new EventPublishedHandler(db, contract, new FixedTimeProvider(DateTimeOffset.UtcNow));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(integrationEvent, CancellationToken.None));

        Assert.Equal(1, contract.CallCount);
        Assert.Contains(integrationEvent.ManifestId.ToString(), exception.Message);
        Assert.False(await db.PublishedEventReferences
            .AnyAsync(r => r.EventId == integrationEvent.EventId));
    }

    [Fact]
    public async Task Handle_WhenManifestAvailable_CreatesReferenceAndInventory()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var integrationEvent = CreateIntegrationEvent();
        var now = new DateTimeOffset(2026, 6, 6, 10, 30, 0, TimeSpan.Zero);

        var contract = new RecordingEventsModuleContract(CreateManifestExport());
        var handler = new EventPublishedHandler(db, contract, new FixedTimeProvider(now));

        await handler.Handle(integrationEvent, CancellationToken.None);

        Assert.Equal(1, contract.CallCount);

        var reference = Assert.Single(await db.PublishedEventReferences
            .Where(r => r.EventId == integrationEvent.EventId).ToListAsync());
        Assert.Equal(integrationEvent.EventId, reference.EventId);
        Assert.Equal(integrationEvent.ManifestId, reference.ManifestId);
        Assert.Equal(now, reference.SyncedAt);

        var inventory = Assert.Single(await db.Inventories
            .Include(i => i.Seats)
            .Include(i => i.Pools)
            .Where(i => i.EventReferenceId == reference.Id).ToListAsync());
        Assert.Equal(reference.Id, inventory.EventReferenceId);
        Assert.Single(inventory.Seats);
        Assert.Single(inventory.Pools);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static EventPublishedIntegrationEvent CreateIntegrationEvent() =>
        new(
            MessageId: Guid.CreateVersion7(),
            EventId: Guid.CreateVersion7(),
            ManifestId: Guid.CreateVersion7(),
            OccurredOn: new DateTimeOffset(2026, 6, 6, 9, 0, 0, TimeSpan.Zero));

    private static ManifestExportDto CreateManifestExport() =>
        new(
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
