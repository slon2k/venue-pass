using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;
using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing.Features.CreateOffer;

public sealed class CreateOfferHandler(
    TicketingDbContext db,
    IValidator<CreateOfferCommand> validator,
    ILogger<CreateOfferHandler> logger)
{
    public async Task<Result<CreateOfferResult>> Handle(
        CreateOfferCommand command,
        CancellationToken ct)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            return CreateOfferErrors.InvalidData(
                [.. validationResult.Errors.Select(e =>
                    new ValidationErrorDetail(e.PropertyName, e.ErrorMessage))]);
        }

        PublishedEventReference? reference = await db.PublishedEventReferences
            .FirstOrDefaultAsync(r => r.EventId == command.EventId, ct);

        if (reference is null)
        {
            return CreateOfferErrors.EventNotPublished(command.EventId);
        }

        Inventory? inventory = await db.Inventories
            .FirstOrDefaultAsync(i => i.EventReferenceId == reference.Id, ct);

        if (inventory is null)
        {
            return CreateOfferErrors.InventoryNotFound(command.EventId);
        }

        Offer offer;

        try
        {
            offer = Offer.Create(
                inventory.Id,
                new OfferName(command.Name),
                new DateTimeRange(command.SaleStart, command.SaleEnd),
                new Currency(command.Currency));
        }
        catch (DomainRuleViolationException ex)
        {
            logger.LogInformation(ex, "Domain validation rejected offer creation.");
            return CreateOfferErrors.InvalidData(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogInformation(ex, "Value object validation rejected offer creation.");
            return CreateOfferErrors.InvalidData(ex.Message);
        }

        db.Offers.Add(offer);
        await db.SaveChangesAsync(ct);

        return new CreateOfferResult(offer.Id);
    }
}

public sealed record CreateOfferCommand(
    Guid EventId,
    string Name,
    string Currency,
    DateTimeOffset? SaleStart,
    DateTimeOffset? SaleEnd);

public sealed record CreateOfferResult(Guid OfferId);
