using VenuePass.BuildingBlocks.Domain;

namespace VenuePass.Modules.Events.Domain.Events;

public static class EventErrors
{
    public static DomainError EventDateMustBeInTheFuture() => new(
        "Events.Event.DateMustBeInTheFuture",
        "Event date must be in the future.");

    public static DomainError EventDateMustBeInTheFutureToPublish() => new(
        "Events.Event.DateMustBeInTheFutureToPublish",
        "Event date must be in the future to publish.");

    public static DomainError EventMustBeInDraftStateToPublish() => new(
        "Events.Event.MustBeInDraftStateToPublish",
        "Event must be in draft state to be published.");
}
