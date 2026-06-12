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

    public static DomainError SomeSeatsNotFound(IEnumerable<InventorySeatId> seatIds) => new(
        "Ticketing.Inventory.SomeSeatsNotFound",
        $"Some of the specified seat IDs were not found in the inventory: {string.Join(", ", seatIds.Select(id => id.Value))}");

    public static DomainError GeneralAdmissionPoolNotFound(GeneralAdmissionPoolId poolId) => new(
        "Ticketing.Inventory.GeneralAdmissionPoolNotFound",
        $"General admission pool with ID '{poolId.Value}' was not found in the inventory.");

    public static DomainError SeatNotAvailable(InventorySeatId seatId) => new(
        "Ticketing.Inventory.SeatNotAvailable",
        $"Seat with ID '{seatId.Value}' is not available for reservation.");

    public static DomainError NotEnoughGeneralAdmissionPoolCapacity(GeneralAdmissionPoolId poolId, int requested, int available) => new(
        "Ticketing.Inventory.NotEnoughGeneralAdmissionPoolCapacity",
        $"Not enough capacity in the general admission pool with ID '{poolId.Value}'. Requested: {requested}, Available: {available}.");

    public static DomainError SeatNotReserved(InventorySeatId seatId) => new(
        "Ticketing.Inventory.SeatNotReserved",
        $"Seat with ID '{seatId.Value}' is not reserved and cannot be released.");

    public static DomainError NotEnoughReservedGeneralAdmissionPoolQuantity(GeneralAdmissionPoolId poolId, int requested, int reserved) => new(
        "Ticketing.Inventory.NotEnoughReservedGeneralAdmissionPoolQuantity",
        $"Not enough reserved quantity in the general admission pool with ID '{poolId.Value}' to release. Requested: {requested}, Reserved: {reserved}.");
}