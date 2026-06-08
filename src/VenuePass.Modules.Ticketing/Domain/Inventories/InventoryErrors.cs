using VenuePass.BuildingBlocks.Domain;

namespace VenuePass.Modules.Ticketing.Domain.Inventories;

public static class InventoryErrors
{
    public static DomainError MustContainInventoryItems() => new(
        "Ticketing.Inventory.MustContainInventoryItems",
        "Inventory must contain at least one seat or general admission pool.");

    public static DomainError DuplicateSourceSeats() => new(
        "Ticketing.Inventory.DuplicateSourceSeats",
        "Inventory manifest contains duplicate source seat IDs.");

    public static DomainError DuplicateSourceGeneralAdmissionAreas() => new(
        "Ticketing.Inventory.DuplicateSourceGeneralAdmissionAreas",
        "Inventory manifest contains duplicate source general admission area IDs.");
}