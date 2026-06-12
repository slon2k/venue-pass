using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.ConfigurePricing;

public sealed class ConfigurePricingHandler(
    TicketingDbContext db,
    IValidator<ConfigurePricingCommand> validator,
    ILogger<ConfigurePricingHandler> logger)
{
    public async Task<Result<ConfigurePricingResult>> Handle(
        ConfigurePricingCommand command,
        CancellationToken ct)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            return ConfigurePricingErrors.InvalidData(
                [.. validationResult.Errors.Select(e =>
                    new ValidationErrorDetail(e.PropertyName, e.ErrorMessage))]);
        }

        Offer? offer = await db.Offers
            .FirstOrDefaultAsync(o => o.Id == new OfferId(command.OfferId), ct);

        if (offer is null)
        {
            return ConfigurePricingErrors.OfferNotFound(command.OfferId);
        }

        Inventory? inventory = await db.Inventories
            .FirstOrDefaultAsync(i => i.Id == offer.InventoryId, ct);

        if (inventory is null)
        {
            throw new InvalidOperationException(
                $"Inventory '{offer.InventoryId}' referenced by offer '{offer.Id}' was not found.");
        }

        var inputs = command.PriceZones
            .Select(item => new PriceZoneInput(
                new PriceZoneName(item.Name),
                new Amount(item.Price),
                item.SeatIds.Select(id => new PriceZoneInventorySeatItemInput(new InventorySeatId(id))).ToArray(),
                item.PoolIds.Select(id => new PriceZoneGeneralAdmissionPoolItemInput(new GeneralAdmissionPoolId(id))).ToArray()
            ))
            .ToArray();

        try
        {
            offer.SetPriceZones(inventory, inputs);
        }
        catch (DomainRuleViolationException ex)
        {
            logger.LogInformation(ex, "Domain validation rejected price zone configuration.");
            return ConfigurePricingErrors.InvalidData(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogInformation(ex, "Value object validation rejected price zone configuration.");
            return ConfigurePricingErrors.InvalidData(ex.Message);
        }

        await db.SaveChangesAsync(ct);

        return new ConfigurePricingResult();
    }
}

public sealed record ConfigurePricingCommand(
    Guid OfferId,
    IReadOnlyList<PriceZoneCommandItem> PriceZones);

public sealed record PriceZoneCommandItem(
    string Name,
    decimal Price,
    IReadOnlyList<Guid> SeatIds,
    IReadOnlyList<Guid> PoolIds);

public sealed record ConfigurePricingResult;
