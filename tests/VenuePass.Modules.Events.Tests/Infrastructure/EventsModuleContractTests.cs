using Microsoft.EntityFrameworkCore;

using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Events.Tests.Infrastructure;

public sealed class EventsModuleContractTests
{
	[Fact]
	public async Task GetManifestForTicketingAsync_WhenManifestDoesNotExist_ReturnsNull()
	{
		// Arrange
		await using var db = CreateDbContext();
		var contract = new EventsModuleContract(db);

		// Act
		var result = await contract.GetManifestForTicketingAsync(ManifestId.Create(), CancellationToken.None);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetManifestForTicketingAsync_WhenManifestIsNotFrozen_ReturnsNull()
	{
		// Arrange
		await using var db = CreateDbContext();
		var contract = new EventsModuleContract(db);
		var manifest = CreateManifest(isFrozen: false);

		db.Manifests.Add(manifest);
		await db.SaveChangesAsync();

		// Act
		var result = await contract.GetManifestForTicketingAsync(manifest.Id, CancellationToken.None);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetManifestForTicketingAsync_WhenManifestIsFrozen_ReturnsMappedExport()
	{
		// Arrange
		await using var db = CreateDbContext();
		var contract = new EventsModuleContract(db);
		var manifest = CreateManifest(isFrozen: true);

		var expectedManifestId = manifest.Id;
		var expectedEventId = manifest.EventId;
		var expectedSection = manifest.Sections[0];
		var expectedRow = expectedSection.Rows[0];
		var expectedSeat = expectedRow.Seats[0];
		var expectedArea = manifest.GeneralAdmissionAreas[0];

		db.Manifests.Add(manifest);
		await db.SaveChangesAsync();

		// Act
		var result = await contract.GetManifestForTicketingAsync(manifest.Id, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(expectedManifestId, result!.ManifestId);
		Assert.Equal(expectedEventId, result.EventId);

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

	private static EventsDbContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<EventsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new EventsDbContext(options);
	}

	private static Manifest CreateManifest(bool isFrozen)
	{
		var venueId = VenueId.Create();
		var eventId = EventId.Create();
		var template = ManifestTemplate.Create(
			new ManifestTemplateName("Ticketing Manifest"),
			null,
			venueId,
			[new SectionDraft("Main", [new RowDraft("A", [new SeatDraft("1")])])],
			[new GeneralAdmissionAreaDraft("Floor", 250)]);

		var manifest = Manifest.CreateFromTemplate(eventId, template);

		if (isFrozen)
		{
			manifest.Freeze();
		}

		return manifest;
	}
}