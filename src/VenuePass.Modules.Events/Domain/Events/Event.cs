using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Venues;

namespace VenuePass.Modules.Events.Domain.Events;

public sealed class Event : AggregateRoot<EventId>
{
    public ManifestId ManifestId { get; private set; }

    public VenueId VenueId { get; private set; }

    public EventName Name { get; private set; } = null!;

    public DateTimeOffset EventDate { get; private set; }

    public EventDescription? Description { get; private set; }

    public EventState State { get; private set; } = EventState.Draft;

    public UserId AssignedManagerId { get; private set; }

    private Event()
    {
    }

    private Event(
        EventId id,
        VenueId venueId,
        ManifestId manifestId,
        EventName name,
        DateTimeOffset eventDate,
        EventDescription? description,
        UserId assignedManagerId)
        : base(id)
    {
        VenueId = venueId;
        ManifestId = manifestId;
        Name = name;
        EventDate = eventDate;
        Description = description;
        AssignedManagerId = assignedManagerId;
    }

    public static Event Create(
        EventId id,
        VenueId venueId,
        ManifestId manifestId,
        EventName name,
        DateTimeOffset eventDate,
        EventDescription? description,
        UserId assignedManagerId,
        TimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (eventDate <= dateTimeProvider.GetUtcNow())
        {
            throw new DomainRuleViolationException(EventErrors.EventDateMustBeInTheFuture());
        }

        return new(
            id,
            venueId,
            manifestId,
            name,
            eventDate,
            description,
            assignedManagerId);
    }

    public void ReassignManager(UserId newManagerId)
    {
        AssignedManagerId = newManagerId;
    }

    public void Publish(TimeProvider dateTimeProvider)
    {
        if (State != EventState.Draft)
        {
            throw new DomainRuleViolationException(EventErrors.EventMustBeInDraftStateToPublish());
        }

        if (EventDate <= dateTimeProvider.GetUtcNow())
        {
            throw new DomainRuleViolationException(EventErrors.EventDateMustBeInTheFutureToPublish());
        }

        State = EventState.Published;

        AddDomainEvent(new EventPublishedDomainEvent(Id, ManifestId));
    }
}

public readonly record struct EventId(Guid Value)
{
    public static EventId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(EventId id) => id.Value;
    public override string ToString() => Value.ToString();
};

public sealed record EventName
{
    public const int MaxLength = 200;
    public string Value { get; private set; }

    public EventName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(EventName name) => name.Value;

    public override string ToString() => Value;
}

public sealed record EventDescription
{
    public const int MaxLength = 1000;
    public string Value { get; private set; }

    public EventDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(EventDescription description) => description.Value;

    public override string ToString() => Value;
}