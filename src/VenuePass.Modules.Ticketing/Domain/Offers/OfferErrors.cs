using VenuePass.BuildingBlocks.Domain;

namespace VenuePass.Modules.Ticketing.Domain.Offers;

public static class OfferErrors
{
    public static DomainError CanOnlySetPriceZonesInDraftStatus() => new(
        "Ticketing.Offer.CanOnlySetPriceZonesInDraftStatus",
        "Price zones can only be set when offer is in draft status.");

    public static DomainError CanOnlyActivateOfferInDraftStatus() => new(
        "Ticketing.Offer.CanOnlyActivateOfferInDraftStatus",
        "Only offers in draft status can be activated.");

    public static DomainError OfferMustHaveAtLeastOnePriceZoneToActivate() => new(
        "Ticketing.Offer.OfferMustHaveAtLeastOnePriceZoneToActivate",
        "Offer must have at least one price zone to be activated.");
    
    public static DomainError PriceZoneMustHaveAtLeastOneItem() => new(
        "Ticketing.Offer.PriceZoneMustHaveAtLeastOneItem",
        "Price zone must have at least one item.");

    public static DomainError PriceZoneCannotHaveDuplicateTargets() => new(
        "Ticketing.Offer.PriceZoneCannotHaveDuplicateTargets",
        "Price zone cannot have duplicate targets.");

    public static DomainError SeatNotInInventory(Guid seatId) => new(
        "Ticketing.Offer.SeatNotInInventory",
        $"Seat with ID '{seatId}' is not part of the inventory.");

    public static DomainError GeneralAdmissionPoolNotInInventory(Guid poolId) => new(
        "Ticketing.Offer.GeneralAdmissionPoolNotInInventory",
        $"General admission pool with ID '{poolId}' is not part of the inventory.");

    public static DomainError InventoryMismatch(Guid passedInventoryId, Guid expectedInventoryId) => new(
        "Ticketing.Offer.InventoryMismatch",
        $"Inventory '{passedInventoryId}' does not match the offer's expected inventory '{expectedInventoryId}'.");

    public static DomainError InventorySeatAlreadyAssignedToAnotherPriceZone(Guid seatId) => new(
        "Ticketing.Offer.InventorySeatAlreadyAssignedToAnotherPriceZone",
        $"Inventory seat with ID '{seatId}' is already assigned to another price zone.");

    public static DomainError GeneralAdmissionPoolAlreadyAssignedToAnotherPriceZone(Guid poolId) => new(
        "Ticketing.Offer.GeneralAdmissionPoolAlreadyAssignedToAnotherPriceZone",
        $"General admission pool with ID '{poolId}' is already assigned to another price zone.");
}