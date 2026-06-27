using VenuePass.BuildingBlocks.Domain;

namespace VenuePass.Modules.Attendance.Domain.PublishedEvents;

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
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));

        if (manifestId == Guid.Empty)
            throw new ArgumentException("Manifest ID cannot be empty.", nameof(manifestId));

        if (syncedAt == default)
            throw new ArgumentException("Synced timestamp cannot be the default value.", nameof(syncedAt));

        EventId = eventId;
        ManifestId = manifestId;
        SyncedAt = syncedAt;
    }

    public static PublishedEventReference Create(
        PublishedEventReferenceId id,
        Guid eventId,
        Guid manifestId,
        DateTimeOffset syncedAt) => new(
            id,
            eventId,
            manifestId,
            syncedAt);
}

public readonly record struct PublishedEventReferenceId(Guid Value)
{
    public static implicit operator Guid(PublishedEventReferenceId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}