namespace VenuePass.BuildingBlocks.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOn { get; }
}
