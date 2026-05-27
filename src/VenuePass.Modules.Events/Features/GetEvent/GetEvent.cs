using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Infrastructure;

namespace VenuePass.Modules.Events.Features.GetEvent;

public sealed class GetEventHandler(EventsDbContext db)
{
    public async Task<Result<GetEventResult>> Handle(
        GetEventQuery query,
        CancellationToken ct)
    {
        var eventId = new EventId(query.EventId);

        var @event = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (@event is null)
        {
            return GetEventErrors.EventNotFound(query.EventId);
        }

        Manifest? manifest = await db.Manifests
            .AsNoTracking()
            .Include(m => m.Sections)
            .ThenInclude(s => s.Rows)
            .ThenInclude(r => r.Seats)
            .Include(m => m.GeneralAdmissionAreas)
            .FirstOrDefaultAsync(m => m.EventId == eventId, ct);

        return ToResult(@event, manifest);
    }

    private static GetEventResult ToResult(Event @event, Manifest? manifest) => new(
        EventId: @event.Id,
        VenueId: @event.VenueId,
        ManifestId: @event.ManifestId,
        Name: @event.Name,
        EventDate: @event.EventDate,
        Description: @event.Description?.Value,
        State: @event.State.ToString(),
        AssignedManagerId: @event.AssignedManagerId,
        Manifest: manifest is null ? null : ToManifestResult(manifest));

    private static GetEventManifestResult ToManifestResult(Manifest manifest) => new(
        ManifestId: manifest.Id,
        Name: manifest.Name,
        Sections:
        [
            .. manifest.Sections.Select(section => new GetEventSectionResult(
                Name: section.Name,
                Rows:
                [
                    .. section.Rows.Select(row => new GetEventRowResult(
                        Label: row.Label,
                        Seats:
                        [
                            .. row.Seats.Select(seat => new GetEventSeatResult(seat.Label))
                        ]))
                ]))
        ],
        GeneralAdmissionAreas:
        [
            .. manifest.GeneralAdmissionAreas.Select(area => new GetEventGeneralAdmissionAreaResult(
                Name: area.Name,
                Capacity: area.Capacity))
        ]);
}

public sealed record GetEventQuery(Guid EventId);

public sealed record GetEventResult(
    Guid EventId,
    Guid VenueId,
    Guid ManifestId,
    string Name,
    DateTimeOffset EventDate,
    string? Description,
    string State,
    Guid AssignedManagerId,
    GetEventManifestResult? Manifest);

public sealed record GetEventManifestResult(
    Guid ManifestId,
    string Name,
    IReadOnlyList<GetEventSectionResult> Sections,
    IReadOnlyList<GetEventGeneralAdmissionAreaResult> GeneralAdmissionAreas);

public sealed record GetEventSectionResult(
    string Name,
    IReadOnlyList<GetEventRowResult> Rows);

public sealed record GetEventRowResult(
    string Label,
    IReadOnlyList<GetEventSeatResult> Seats);

public sealed record GetEventSeatResult(string Label);

public sealed record GetEventGeneralAdmissionAreaResult(
    string Name,
    int Capacity);
