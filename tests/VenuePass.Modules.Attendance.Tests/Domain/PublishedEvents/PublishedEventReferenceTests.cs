using VenuePass.Modules.Attendance.Domain.PublishedEvents;

using Xunit;

namespace VenuePass.Modules.Attendance.Tests.Domain.PublishedEvents;

public sealed class PublishedEventReferenceTests
{
    [Fact]
    public void Create_WithValidInput_CreatesReference()
    {
        var id = new PublishedEventReferenceId(Guid.CreateVersion7());
        var eventId = Guid.CreateVersion7();
        var manifestId = Guid.CreateVersion7();
        var syncedAt = new DateTimeOffset(2026, 6, 27, 16, 0, 0, TimeSpan.Zero);

        var reference = PublishedEventReference.Create(id, eventId, manifestId, syncedAt);

        Assert.Equal(id, reference.Id);
        Assert.Equal(eventId, reference.EventId);
        Assert.Equal(manifestId, reference.ManifestId);
        Assert.Equal(syncedAt, reference.SyncedAt);
    }

    [Fact]
    public void Create_WithEmptyEventId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            PublishedEventReference.Create(
                id: new PublishedEventReferenceId(Guid.CreateVersion7()),
                eventId: Guid.Empty,
                manifestId: Guid.CreateVersion7(),
                syncedAt: DateTimeOffset.UtcNow));

        Assert.Contains("Event ID cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithEmptyManifestId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            PublishedEventReference.Create(
                id: new PublishedEventReferenceId(Guid.CreateVersion7()),
                eventId: Guid.CreateVersion7(),
                manifestId: Guid.Empty,
                syncedAt: DateTimeOffset.UtcNow));

        Assert.Contains("Manifest ID cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithDefaultSyncedAt_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            PublishedEventReference.Create(
                id: new PublishedEventReferenceId(Guid.CreateVersion7()),
                eventId: Guid.CreateVersion7(),
                manifestId: Guid.CreateVersion7(),
                syncedAt: default));

        Assert.Contains("Synced timestamp cannot be the default value", exception.Message);
    }
}