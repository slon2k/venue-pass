namespace VenuePass.BuildingBlocks.Messaging;

public interface IIntegrationEvent
{
    Guid MessageId { get; }

    DateTimeOffset OccurredOn { get; }
}

public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task Handle(TEvent integrationEvent, CancellationToken cancellationToken);
}
