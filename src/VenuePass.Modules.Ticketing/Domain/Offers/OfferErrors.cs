using VenuePass.BuildingBlocks.Domain;

namespace VenuePass.Modules.Ticketing.Domain.Offers;

public static class OfferErrors
{
    public static DomainError CanOnlySetPriceLevelsInDraftStatus() => new(
        "Ticketing.Offer.CanOnlySetPriceLevelsInDraftStatus",
        "Price levels can only be set when offer is in draft status.");

    public static DomainError CanOnlyActivateOfferInDraftStatus() => new(
        "Ticketing.Offer.CanOnlyActivateOfferInDraftStatus",
        "Only offers in draft status can be activated.");

    public static DomainError OfferMustHaveAtLeastOnePriceLevelToActivate() => new(
        "Ticketing.Offer.OfferMustHaveAtLeastOnePriceLevelToActivate",
        "Offer must have at least one price level to be activated.");
    
    public static DomainError PriceLevelMustHaveAtLeastOneItem() => new(
        "Ticketing.Offer.PriceLevelMustHaveAtLeastOneItem",
        "Price level must have at least one item.");

    public static DomainError PriceLevelCannotHaveDuplicateTargets() => new(
        "Ticketing.Offer.PriceLevelCannotHaveDuplicateTargets",
        "Price level cannot have duplicate targets.");

    public static DomainError MustProvideAtLeastOnePriceLevel() => new(
        "Ticketing.Offer.MustProvideAtLeastOnePriceLevel",
        "At least one price level must be provided.");
}