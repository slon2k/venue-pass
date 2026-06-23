using Microsoft.EntityFrameworkCore;

using VenuePass.Modules.Ticketing.Features.EventPublished;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Features.EventPublished;

public sealed class EventPublishedHandlerTests
{
    [Fact]
    public void IsDuplicatePublishedEventReference_WhenIndexNameAppearsInMessage_ReturnsTrue()
    {
        // Arrange
        var exception = new DbUpdateException(
            "Save failed.",
            new InvalidOperationException("Violation of UNIQUE KEY constraint 'IX_published_event_references_event_id'."));

        // Act
        bool isDuplicate = EventPublishedHandler.IsDuplicatePublishedEventReference(exception);

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public void IsDuplicatePublishedEventReference_WhenNoDuplicateSignal_ReturnsFalse()
    {
        // Arrange
        var exception = new DbUpdateException("Save failed.", new InvalidOperationException("Some other DB error."));

        // Act
        bool isDuplicate = EventPublishedHandler.IsDuplicatePublishedEventReference(exception);

        // Assert
        Assert.False(isDuplicate);
    }

}