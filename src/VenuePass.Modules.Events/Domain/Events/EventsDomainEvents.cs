using VenuePass.BuildingBlocks.Domain;

namespace VenuePass.Modules.Events.Domain.Events;

public record EventPublishedDomainEvent : DomainEvent
{
    public EventId EventId { get; init; }

    public EventPublishedDomainEvent(EventId eventId)
    {
        EventId = eventId;
    }
}

