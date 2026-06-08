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

    public static DomainError SeatNotInInventory(Guid seatId) => new(
        "Ticketing.Offer.SeatNotInInventory",
        $"Seat with ID '{seatId}' is not part of the inventory.");

    public static DomainError GeneralAdmissionPoolNotInInventory(Guid poolId) => new(
        "Ticketing.Offer.GeneralAdmissionPoolNotInInventory",
        $"General admission pool with ID '{poolId}' is not part of the inventory.");

    public static DomainError InventoryMismatch(Guid passedInventoryId, Guid expectedInventoryId) => new(
        "Ticketing.Offer.InventoryMismatch",
        $"Inventory '{passedInventoryId}' does not match the offer's expected inventory '{expectedInventoryId}'.");
}