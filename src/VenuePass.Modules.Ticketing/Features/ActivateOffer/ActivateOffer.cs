using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.ActivateOffer;

public sealed class ActivateOfferHandler(
    TicketingDbContext db,
    ILogger<ActivateOfferHandler> logger)
{
    public async Task<Result<ActivateOfferResult>> Handle(ActivateOfferCommand command, CancellationToken ct)
    {
        Offer? offer = await db.Offers.FirstOrDefaultAsync(o => o.Id == new OfferId(command.OfferId), ct);

        if (offer is null)
            return ActivateOfferErrors.OfferNotFound(command.OfferId);

        try
        {
            offer.Activate();
        }
        catch (DomainException ex)
        {
            logger.LogInformation(ex, "Domain rule rejected offer activation.");
            return Error.FromDomainException(ex);
        }

        await db.SaveChangesAsync(ct);
        return new ActivateOfferResult();
    }
}

public sealed record ActivateOfferCommand(Guid OfferId);
public sealed record ActivateOfferResult;
