using System.Security.Cryptography;

using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Ticketing.Domain.Tickets;

namespace VenuePass.Modules.Ticketing.Infrastructure;

internal sealed class TicketCodeGenerator : ITicketCodeGenerator
{
    private const string CrockfordBase32Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public const int MaxBatchSize = 1000;


    public TicketCode Generate()
    {
        Span<char> chars = stackalloc char[TicketCode.Length];

        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = CrockfordBase32Alphabet[RandomNumberGenerator.GetInt32(CrockfordBase32Alphabet.Length)];
        }

        return new TicketCode(new string(chars));
    }
}