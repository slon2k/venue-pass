using VenuePass.BuildingBlocks.Domain;
using Xunit;

namespace VenuePass.BuildingBlocks.Tests;

public sealed class DateTimeRangeTests
{
    [Fact]
    public void Constructor_WhenStartIsAfterEnd_ThrowsArgumentException()
    {
        var start = new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero);
        var end = start.AddMinutes(-1);

        void Act() => _ = new DateTimeRange(start, end);

        Assert.Throws<ArgumentException>(Act);
    }

    [Fact]
    public void Contains_WhenRangeIsUnbounded_ReturnsTrue()
    {
        var range = new DateTimeRange(start: null, end: null);

        var result = range.Contains(new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero));

        Assert.True(result);
    }

    [Fact]
    public void Contains_WhenOnlyEndIsSet_ReturnsTrueForDatesBeforeOrAtEnd()
    {
        var end = new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero);
        var range = new DateTimeRange(start: null, end);

        Assert.True(range.Contains(end.AddMinutes(-1)));
        Assert.True(range.Contains(end));
        Assert.False(range.Contains(end.AddMinutes(1)));
    }

    [Fact]
    public void Contains_WhenOnlyStartIsSet_ReturnsTrueForDatesAfterOrAtStart()
    {
        var start = new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero);
        var range = new DateTimeRange(start, end: null);

        Assert.False(range.Contains(start.AddMinutes(-1)));
        Assert.True(range.Contains(start));
        Assert.True(range.Contains(start.AddMinutes(1)));
    }

    [Fact]
    public void Contains_WhenBothBoundsAreSet_IsInclusive()
    {
        var start = new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);
        var range = new DateTimeRange(start, end);

        Assert.False(range.Contains(start.AddMinutes(-1)));
        Assert.True(range.Contains(start));
        Assert.True(range.Contains(start.AddMinutes(30)));
        Assert.True(range.Contains(end));
        Assert.False(range.Contains(end.AddMinutes(1)));
    }
}
