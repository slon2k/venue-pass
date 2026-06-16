using VenuePass.Modules.Ticketing.Domain.Tickets;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Infrastructure;

public sealed class TicketCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsCodeWithExpectedLengthAndAlphabet()
    {
        var generator = new TicketCodeGenerator();

        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        for (int i = 0; i < 100; i++)
        {
            var code = generator.Generate();

            Assert.Equal(TicketCode.Length, code.Value.Length);
            Assert.All(code.Value, c => Assert.Contains(c, alphabet));
        }
    }
}
