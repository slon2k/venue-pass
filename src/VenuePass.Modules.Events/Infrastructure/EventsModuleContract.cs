using Microsoft.EntityFrameworkCore;

using VenuePass.Modules.Events.Contracts;
using VenuePass.Modules.Events.Domain.Manifests;

namespace VenuePass.Modules.Events.Infrastructure;

public sealed class EventsModuleContract(EventsDbContext db) : IEventsModuleContract
{
    public async Task<ManifestExportDto?> GetManifestForTicketingAsync(Guid manifestId, CancellationToken ct)
    {
        var manifest = await db.Manifests
            .Include(m => m.Sections)
                .ThenInclude(s => s.Rows)
                    .ThenInclude(r => r.Seats)
            .Include(m => m.GeneralAdmissionAreas)
            .FirstOrDefaultAsync(m => m.Id == manifestId, ct);

        return manifest switch
        {
            null => null,
            { IsFrozen: false } => null,
            _ => MapToExport(manifest)
        };
    }

    private static ManifestExportDto MapToExport(Manifest manifest)
    {
        return new ManifestExportDto(
            manifest.Id,
            manifest.EventId,
            [.. manifest.Sections.Select(s => new SectionExportDto(
                s.Id,
                s.Name,
                [.. s.Rows.Select(r => new RowExportDto(
                    r.Id,
                    r.Label,
                    [.. r.Seats.Select(seat => new SeatExportDto(
                        seat.Id,
                        seat.Label))]))]))],
            [.. manifest.GeneralAdmissionAreas.Select(ga => new GeneralAdmissionAreaExportDto(
                ga.Id,
                ga.Name,
                ga.Capacity))]);
    }
}