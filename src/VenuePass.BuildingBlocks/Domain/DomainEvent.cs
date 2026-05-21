namespace VenuePass.BuildingBlocks.Domain;

public abstract record DomainEvent
{
    public Guid DomainEventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
