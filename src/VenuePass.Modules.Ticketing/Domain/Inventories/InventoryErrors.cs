using VenuePass.BuildingBlocks.Domain;

namespace VenuePass.Modules.Ticketing.Domain.Inventories;

public static class InventoryErrors
{
    public static DomainError MustContainStockItems() => new(
        "Ticketing.Inventory.MustContainStockItems",
        "Inventory must contain at least one seat or general admission pool.");
}