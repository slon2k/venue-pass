using VenuePass.Modules.Ticketing.Domain.Tickets;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Tickets;

public sealed class TicketCodeTests
{
    [Fact]
    public void Constructor_NormalizesWhitespaceCaseAndHyphens()
    {
        var code = new TicketCode("  abcd-efgh-jkmn-pqrs  ");

        Assert.Equal("ABCDEFGHJKMNPQRS", code.Value);
    }

    [Fact]
    public void Constructor_WhenLengthIsNot16_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TicketCode("ABCDEF"));
    }

    [Fact]
    public void Constructor_WhenContainsNonCrockfordCharacter_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TicketCode("ABCDEFGHJKMNPQRI"));
    }
}
