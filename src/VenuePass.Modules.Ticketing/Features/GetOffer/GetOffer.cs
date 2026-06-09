using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.GetOffer;

public sealed class GetOfferHandler(TicketingDbContext db)
{
    public async Task<Result<GetOfferResult>> Handle(
        GetOfferQuery query,
        CancellationToken ct)
    {
        var offer = await db.Offers
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == new OfferId(query.OfferId), ct);

        if (offer is null)
        {
            return GetOfferErrors.OfferNotFound(query.OfferId);
        }

        return ToResult(offer);
    }

    private static GetOfferResult ToResult(Offer offer) => new(
        OfferId: offer.Id.Value,
        InventoryId: offer.InventoryId.Value,
        Name: offer.Name.Value,
        Currency: offer.Currency.Value,
        Status: offer.Status.ToString(),
        SaleStart: offer.SalesRange.Start,
        SaleEnd: offer.SalesRange.End,
        PriceZones:
        [
            .. offer.PriceZones.Select(zone => new GetOfferPriceZoneResult(
                PriceZoneId: zone.Id.Value,
                Name: zone.Name.Value,
                Price: zone.Price.Value,
                SeatIds: [.. zone.InventorySeatItems.Select(i => i.InventorySeatId.Value)],
                PoolIds: [.. zone.GeneralAdmissionPoolItems.Select(i => i.GeneralAdmissionPoolId.Value)]))
        ]);
}

public sealed record GetOfferQuery(Guid OfferId);

public sealed record GetOfferResult(
    Guid OfferId,
    Guid InventoryId,
    string Name,
    string Currency,
    string Status,
    DateTimeOffset? SaleStart,
    DateTimeOffset? SaleEnd,
    IReadOnlyList<GetOfferPriceZoneResult> PriceZones);

public sealed record GetOfferPriceZoneResult(
    Guid PriceZoneId,
    string Name,
    decimal Price,
    IReadOnlyList<Guid> SeatIds,
    IReadOnlyList<Guid> PoolIds);
