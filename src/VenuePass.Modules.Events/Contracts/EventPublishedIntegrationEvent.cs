using VenuePass.BuildingBlocks.Messaging;

namespace VenuePass.Modules.Events.Contracts;

public sealed record EventPublishedIntegrationEvent(
    Guid EventId,
    Guid VenueEventId,
    Guid ManifestId,
    DateTimeOffset OccurredOn) : IIntegrationEvent;
