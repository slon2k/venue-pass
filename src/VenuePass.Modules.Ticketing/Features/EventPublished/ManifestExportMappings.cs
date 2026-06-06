using VenuePass.Modules.Events.Contracts;
using VenuePass.Modules.Ticketing.Domain.Inventories;

namespace VenuePass.Modules.Ticketing.Features.EventPublished;

internal static class ManifestExportMappings
{
    public static InventoryManifest ToInventoryManifest(this ManifestExportDto manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return new InventoryManifest(
            [.. manifest.Sections
                .Select(section => new InventorySectionInput(
                    section.Name,
                    [.. section.Rows
                        .Select(row => new InventoryRowInput(
                            row.Label,
                            [.. row.Seats.Select(seat => new InventorySeatInput(seat.SeatId, seat.Label))]))]))],
            [.. manifest.GeneralAdmissionAreas.Select(area => new InventoryGeneralAdmissionAreaInput(area.AreaId, area.Name, area.Capacity))]);
    }
}