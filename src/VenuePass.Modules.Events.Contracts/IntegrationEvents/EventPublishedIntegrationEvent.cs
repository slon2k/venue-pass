using VenuePass.BuildingBlocks.Messaging;

namespace VenuePass.Modules.Events.Contracts.IntegrationEvents;

public sealed record EventPublishedIntegrationEvent(
    Guid MessageId,
    Guid EventId,
    Guid ManifestId,
    DateTimeOffset OccurredOn) : IIntegrationEvent;
