using VenuePass.BuildingBlocks.Domain;

namespace VenuePass.Modules.Ticketing.Domain.PublishedEvents;

public sealed class PublishedEventReference : Entity<PublishedEventReferenceId>
{    
    public Guid EventId { get; private set; }    
    public Guid ManifestId { get; private set; }

    public DateTimeOffset SyncedAt { get; private set; }

    private PublishedEventReference()
    {
    }

    private PublishedEventReference(
        PublishedEventReferenceId id,
        Guid eventId,
        Guid manifestId,
        DateTimeOffset syncedAt)
        : base(id)
    {
        EventId = eventId;
        ManifestId = manifestId;
        SyncedAt = syncedAt;
    }

    public static PublishedEventReference Create(Guid eventId, Guid manifestId, DateTimeOffset syncedAt) => new(
        PublishedEventReferenceId.Create(),
        eventId,
        manifestId,
        syncedAt);
}

public record PublishedEventReferenceId(Guid Value)
{
    public static PublishedEventReferenceId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(PublishedEventReferenceId id) => id.Value;
    public override string ToString() => Value.ToString();
}