using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Venues;
using Xunit;

namespace VenuePass.Modules.Events.Tests.Domain;

public sealed class EventTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureDate = FixedNow.AddDays(30);
    private static readonly UserId AnyManager = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    private static FakeTimeProvider At(DateTimeOffset now) => new(now);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithFutureDate_ReturnsEventInDraftState()
    {
        var ev = Event.Create(
            EventId.Create(),
            VenueId.Create(),
            ManifestId.Create(),
            new EventName("Summer Concert"),
            FutureDate,
            null,
            AnyManager,
            At(FixedNow));

        Assert.Equal(EventState.Draft, ev.State);
    }

    [Fact]
    public void Create_WithFutureDate_SetsAllProperties()
    {
        var venueId = VenueId.Create();
        var manifestId = ManifestId.Create();

        var ev = Event.Create(
            EventId.Create(),
            venueId,
            manifestId,
            new EventName("Summer Concert"),
            FutureDate,
            new EventDescription("An outdoor concert"),
            AnyManager,
            At(FixedNow));

        Assert.NotEqual(Guid.Empty, ev.Id.Value);
        Assert.Equal(venueId, ev.VenueId);
        Assert.Equal(manifestId, ev.ManifestId);
        Assert.Equal("Summer Concert", ev.Name.Value);
        Assert.Equal(FutureDate, ev.EventDate);
        Assert.Equal("An outdoor concert", ev.Description!.Value);
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        void Act() => Event.Create(
            EventId.Create(),
            VenueId.Create(),
            ManifestId.Create(),
            null!,
            FutureDate,
            null,
            AnyManager,
            At(FixedNow));

        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void Create_WhenEventDateIsInThePast_ThrowsDomainRuleViolationException()
    {
        var pastDate = FixedNow.AddDays(-1);

        void Act() => Event.Create(
            EventId.Create(),
            VenueId.Create(),
            ManifestId.Create(),
            new EventName("Concert"),
            pastDate,
            null,
            AnyManager,
            At(FixedNow));

        var exception = Assert.Throws<DomainRuleViolationException>(Act);
        Assert.Equal("Events.Event.DateMustBeInTheFuture", exception.Code);
    }

    [Fact]
    public void Create_WhenEventDateEqualsNow_ThrowsDomainRuleViolationException()
    {
        void Act() => Event.Create(
            EventId.Create(),
            VenueId.Create(),
            ManifestId.Create(),
            new EventName("Concert"),
            FixedNow,
            null,
            AnyManager,
            At(FixedNow));

        var exception = Assert.Throws<DomainRuleViolationException>(Act);
        Assert.Equal("Events.Event.DateMustBeInTheFuture", exception.Code);
    }

    // ── Publish ───────────────────────────────────────────────────────────────

    [Fact]
    public void Publish_FromDraftState_SetsStateToPublished()
    {
        var ev = CreateDraftEvent();

        ev.Publish(At(FixedNow));

        Assert.Equal(EventState.Published, ev.State);
    }

    [Fact]
    public void Publish_FromDraftState_RaisesEventPublishedDomainEvent()
    {
        var ev = CreateDraftEvent();

        ev.Publish(At(FixedNow));

        Assert.Single(ev.DomainEvents);
        Assert.IsType<EventPublishedDomainEvent>(ev.DomainEvents.Single());
    }

    [Fact]
    public void Publish_FromDraftState_DomainEventCarriesCorrectIds()
    {
        var ev = CreateDraftEvent();

        ev.Publish(At(FixedNow));

        var domainEvent = (EventPublishedDomainEvent)ev.DomainEvents.Single();
        Assert.Equal(ev.Id, domainEvent.EventId);
        Assert.Equal(ev.ManifestId, domainEvent.ManifestId);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ThrowsDomainRuleViolationException()
    {
        var ev = CreateDraftEvent();
        ev.Publish(At(FixedNow));

        void Act() => ev.Publish(At(FixedNow));

        var exception = Assert.Throws<DomainRuleViolationException>(Act);
        Assert.Equal("Events.Event.MustBeInDraftStateToPublish", exception.Code);
    }

    [Fact]
    public void Publish_WhenEventDateIsInThePast_ThrowsDomainRuleViolationException()
    {
        var ev = CreateDraftEvent(eventDate: FutureDate);

        // Advance clock so the event date is now in the past
        var laterClock = At(FutureDate.AddDays(1));

        void Act() => ev.Publish(laterClock);

        var exception = Assert.Throws<DomainRuleViolationException>(Act);
        Assert.Equal("Events.Event.DateMustBeInTheFutureToPublish", exception.Code);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Event CreateDraftEvent(DateTimeOffset? eventDate = null)
    {
        var date = eventDate ?? FutureDate;

        return Event.Create(
            EventId.Create(),
            VenueId.Create(),
            ManifestId.Create(),
            new EventName("Concert"),
            date,
            null,
            AnyManager,
            At(FixedNow));
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
