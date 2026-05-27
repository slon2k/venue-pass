using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Domain.Manifests;

namespace VenuePass.Modules.Events.Domain.Events;

public record EventPublishedDomainEvent(EventId EventId, ManifestId ManifestId) : DomainEvent();