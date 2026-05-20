namespace VenuePass.BuildingBlocks.Domain;

public abstract record DomainEvent
{
    public Guid DomainEventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
