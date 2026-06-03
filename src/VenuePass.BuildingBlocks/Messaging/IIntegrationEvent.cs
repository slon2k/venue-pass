namespace VenuePass.BuildingBlocks.Messaging;

public interface IIntegrationEvent
{
    Guid MessageId { get; }

    DateTimeOffset OccurredOn { get; }
}
