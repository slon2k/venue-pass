using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Infrastructure;

using DomainEvent = VenuePass.Modules.Events.Domain.Events.Event;

using Xunit;

namespace VenuePass.IntegrationTests.Events.Infrastructure;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class EventsModuleContractTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public EventsModuleContractTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetManifestForTicketingAsync_WhenManifestDoesNotExist_ReturnsNull()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        var contract = new EventsModuleContract(db);

        var result = await contract.GetManifestForTicketingAsync(ManifestId.Create(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetManifestForTicketingAsync_WhenManifestIsNotFrozen_ReturnsNull()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        var contract = new EventsModuleContract(db);

        var manifest = await CreateAndSaveManifestAsync(db, isFrozen: false);

        var result = await contract.GetManifestForTicketingAsync(manifest.Id, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetManifestForTicketingAsync_WhenManifestIsFrozen_ReturnsMappedExport()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        var contract = new EventsModuleContract(db);

        var manifest = await CreateAndSaveManifestAsync(db, isFrozen: true);

        var expectedSection = manifest.Sections[0];
        var expectedRow = expectedSection.Rows[0];
        var expectedSeat = expectedRow.Seats[0];
        var expectedArea = manifest.GeneralAdmissionAreas[0];

        var result = await contract.GetManifestForTicketingAsync(manifest.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(manifest.Id, result!.ManifestId);
        Assert.Equal(manifest.EventId, result.EventId);

        var section = Assert.Single(result.Sections);
        Assert.Equal(expectedSection.Id, section.SectionId);
        Assert.Equal(expectedSection.Name.Value, section.Name);

        var row = Assert.Single(section.Rows);
        Assert.Equal(expectedRow.Id, row.RowId);
        Assert.Equal(expectedRow.Label.Value, row.Label);

        var seat = Assert.Single(row.Seats);
        Assert.Equal(expectedSeat.Id, seat.SeatId);
        Assert.Equal(expectedSeat.Label.Value, seat.Label);

        var area = Assert.Single(result.GeneralAdmissionAreas);
        Assert.Equal(expectedArea.Id, area.AreaId);
        Assert.Equal(expectedArea.Name.Value, area.Name);
        Assert.Equal(expectedArea.Capacity.Value, area.Capacity);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<Manifest> CreateAndSaveManifestAsync(EventsDbContext db, bool isFrozen)
    {
        var suffix = Guid.NewGuid().ToString("N");

        var venue = Venue.Create(
            new VenueName($"Test Venue {suffix}"),
            new VenueAddress(new StreetAddress("123 Main St"), new City($"Seattle-{suffix}"), new Country("US")),
            new VenueCapacity(1000));
        var venueId = venue.Id;
        var eventId = EventId.Create();

        var template = ManifestTemplate.Create(
            new ManifestTemplateName("Ticketing Manifest"),
            null,
            venueId,
            [new SectionDraft("Main", [new RowDraft("A", [new SeatDraft("1")])])],
            [new GeneralAdmissionAreaDraft("Floor", 250)]);

        var manifest = Manifest.CreateFromTemplate(eventId, template);

        var timeProvider = TimeProvider.System;
        var @event = DomainEvent.Create(
            eventId,
            venueId,
            manifest.Id,
            new EventName("Test Event"),
            DateTimeOffset.UtcNow.AddDays(30),
            null,
            new BuildingBlocks.Domain.UserId(Guid.NewGuid()),
            timeProvider);

        if (isFrozen)
        {
            manifest.Freeze();
        }

        db.Venues.Add(venue);
        db.Events.Add(@event);
        db.Manifests.Add(manifest);
        await db.SaveChangesAsync();

        return manifest;
    }
}
