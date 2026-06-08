using VenuePass.Modules.Ticketing.Domain.Inventories;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Inventories;

public sealed class InventoryValueObjectsTests
{
    [Fact]
    public void SectionName_WhenValueHasSurroundingWhitespace_TrimsValue()
    {
        var section = new SectionName("  Main Floor  ");

        Assert.Equal("Main Floor", section.Value);
    }

    [Fact]
    public void SectionName_WhenValueExceedsMaxLength_ThrowsArgumentException()
    {
        var value = new string('a', SectionName.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => _ = new SectionName(value));
    }

    [Fact]
    public void RowLabel_WhenValueHasSurroundingWhitespace_TrimsValue()
    {
        var row = new RowLabel("  A  ");

        Assert.Equal("A", row.Value);
    }

    [Fact]
    public void RowLabel_WhenValueExceedsMaxLength_ThrowsArgumentException()
    {
        var value = new string('a', RowLabel.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => _ = new RowLabel(value));
    }

    [Fact]
    public void SeatLabel_WhenValueHasSurroundingWhitespace_TrimsValue()
    {
        var seat = new SeatLabel("  14  ");

        Assert.Equal("14", seat.Value);
    }

    [Fact]
    public void SeatLabel_WhenValueExceedsMaxLength_ThrowsArgumentException()
    {
        var value = new string('a', SeatLabel.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => _ = new SeatLabel(value));
    }

    [Fact]
    public void GeneralAdmissionPoolName_WhenValueHasSurroundingWhitespace_TrimsValue()
    {
        var name = new GeneralAdmissionPoolName("  Floor  ");

        Assert.Equal("Floor", name.Value);
    }

    [Fact]
    public void GeneralAdmissionPoolName_WhenValueExceedsMaxLength_ThrowsArgumentException()
    {
        var value = new string('a', GeneralAdmissionPoolName.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => _ = new GeneralAdmissionPoolName(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GeneralAdmissionPoolCapacity_WhenValueIsNotPositive_ThrowsArgumentOutOfRangeException(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new GeneralAdmissionPoolCapacity(value));
    }

    [Fact]
    public void GeneralAdmissionPoolCapacity_WhenValueIsPositive_SetsValue()
    {
        var capacity = new GeneralAdmissionPoolCapacity(250);

        Assert.Equal(250, capacity.Value);
    }
}
