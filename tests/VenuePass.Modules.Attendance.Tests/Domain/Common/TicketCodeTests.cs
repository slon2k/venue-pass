using VenuePass.Modules.Attendance.Domain.Common;

using Xunit;

namespace VenuePass.Modules.Attendance.Tests.Domain.Common;

public sealed class TicketCodeTests
{
    [Fact]
    public void Constructor_WithHyphenatedLowercaseInput_NormalizesToUppercaseWithoutHyphens()
    {
        var code = new TicketCode("01ar-z3nd-ekts-v4rr");

        Assert.Equal("01ARZ3NDEKTSV4RR", code.Value);
    }

    [Fact]
    public void Constructor_WithInvalidLength_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new TicketCode("1234"));

        Assert.Contains("must be 16 characters long", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidCharacter_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new TicketCode("01ARZ3NDEKTSV4RI"));

        Assert.Contains("contains invalid character", exception.Message);
    }

    [Fact]
    public void TryCreate_WithValidInput_ReturnsTrueAndNormalizedValue()
    {
        var result = TicketCode.TryCreate("01ar-z3nd-ekts-v4rr", out var code);

        Assert.True(result);
        Assert.Equal("01ARZ3NDEKTSV4RR", code.Value);
    }

    [Fact]
    public void TryCreate_WithInvalidInput_ReturnsFalse()
    {
        var result = TicketCode.TryCreate("invalid", out _);

        Assert.False(result);
    }
}