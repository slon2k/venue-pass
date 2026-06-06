using VenuePass.Modules.Events.Contracts;
using VenuePass.Modules.Ticketing.Features.EventPublished;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Features.EventPublished;

public sealed class ManifestExportMappingsTests
{
    [Fact]
    public void ToInventoryManifest_MapsSectionsSeatsAndGeneralAdmissionAreas()
    {
        // Arrange
        var sectionId = Guid.CreateVersion7();
        var rowId = Guid.CreateVersion7();
        var seatId = Guid.CreateVersion7();
        var areaId = Guid.CreateVersion7();
        var manifest = new ManifestExportDto(
            ManifestId: Guid.CreateVersion7(),
            EventId: Guid.CreateVersion7(),
            Sections:
            [
                new SectionExportDto(
                    SectionId: sectionId,
                    Name: "Balcony",
                    Rows:
                    [
                        new RowExportDto(
                            RowId: rowId,
                            Label: "C",
                            Seats:
                            [new SeatExportDto(SeatId: seatId, Label: "14")])
                    ])
            ],
            GeneralAdmissionAreas:
            [new GeneralAdmissionAreaExportDto(AreaId: areaId, Name: "Floor", Capacity: 300)]);

        // Act
        var inventoryManifest = manifest.ToInventoryManifest();

        // Assert
        var section = Assert.Single(inventoryManifest.Sections);
        Assert.Equal("Balcony", section.Name);

        var row = Assert.Single(section.Rows);
        Assert.Equal("C", row.Label);

        var seat = Assert.Single(row.Seats);
        Assert.Equal(seatId, seat.SeatId);
        Assert.Equal("14", seat.Label);

        var area = Assert.Single(inventoryManifest.GeneralAdmissionAreas);
        Assert.Equal(areaId, area.AreaId);
        Assert.Equal("Floor", area.Name);
        Assert.Equal(300, area.Capacity);
    }
}