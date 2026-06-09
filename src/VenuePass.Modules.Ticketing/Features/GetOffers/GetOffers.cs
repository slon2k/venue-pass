using Microsoft.EntityFrameworkCore;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.GetOffers;

public sealed class GetOffersHandler(TicketingDbContext db)
{
    public async Task<Result<GetOffersResult>> Handle(
        GetOffersQuery query,
        CancellationToken ct)
    {
        var reference = await db.PublishedEventReferences
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EventId == query.EventId, ct);

        if (reference is null)
        {
            return GetOffersErrors.EventNotFound(query.EventId);
        }

        var inventory = await db.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.EventReferenceId == reference.Id, ct);

        if (inventory is null)
        {
            return GetOffersErrors.EventNotFound(query.EventId);
        }

        var offers = await db.Offers
            .AsNoTracking()
            .Where(o => o.InventoryId == inventory.Id)
            .ToListAsync(ct);

        return new GetOffersResult(
            Offers: [.. offers.Select(ToItemResult)]);
    }

    private static GetOffersItemResult ToItemResult(Offer offer) => new(
        OfferId: offer.Id.Value,
        InventoryId: offer.InventoryId.Value,
        Name: offer.Name.Value,
        Currency: offer.Currency.Value,
        Status: offer.Status.ToString(),
        SaleStart: offer.SalesRange.Start,
        SaleEnd: offer.SalesRange.End,
        PriceZones:
        [
            .. offer.PriceZones.Select(zone => new GetOffersPriceZoneResult(
                PriceZoneId: zone.Id.Value,
                Name: zone.Name.Value,
                Price: zone.Price.Value,
                SeatIds: [.. zone.InventorySeatItems.Select(i => i.InventorySeatId.Value)],
                PoolIds: [.. zone.GeneralAdmissionPoolItems.Select(i => i.GeneralAdmissionPoolId.Value)]))
        ]);
}

public sealed record GetOffersQuery(Guid EventId);

public sealed record GetOffersResult(IReadOnlyList<GetOffersItemResult> Offers);

public sealed record GetOffersItemResult(
    Guid OfferId,
    Guid InventoryId,
    string Name,
    string Currency,
    string Status,
    DateTimeOffset? SaleStart,
    DateTimeOffset? SaleEnd,
    IReadOnlyList<GetOffersPriceZoneResult> PriceZones);

public sealed record GetOffersPriceZoneResult(
    Guid PriceZoneId,
    string Name,
    decimal Price,
    IReadOnlyList<Guid> SeatIds,
    IReadOnlyList<Guid> PoolIds);
