using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;

using Xunit;

namespace VenuePass.Modules.Ticketing.Tests.Domain.Inventories;

public sealed class InventorySeatReservationTests
{
    [Fact]
    public void Reserve_WhenAvailable_TransitionsToReserved()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        Assert.Equal(SeatAvailability.Available, seat.Availability);

        seat.Reserve();

        Assert.Equal(SeatAvailability.Reserved, seat.Availability);
    }

    [Fact]
    public void Reserve_WhenAlreadyReserved_ThrowsDomainRuleViolation()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        seat.Reserve();

        var exception = Assert.Throws<DomainRuleViolationException>(() => seat.Reserve());

        Assert.Equal(InventoryErrors.SeatNotAvailable(seat.Id).Code, exception.Code);
    }

    [Fact]
    public void Reserve_WhenAlreadySold_ThrowsDomainRuleViolation()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        seat.Reserve();
        seat.Sell();

        var exception = Assert.Throws<DomainRuleViolationException>(() => seat.Reserve());

        Assert.Equal(InventoryErrors.SeatNotAvailable(seat.Id).Code, exception.Code);
    }

    [Fact]
    public void Release_WhenReserved_TransitionsToAvailable()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        seat.Reserve();

        seat.Release();

        Assert.Equal(SeatAvailability.Available, seat.Availability);
    }

    [Fact]
    public void Release_WhenNotReserved_ThrowsDomainRuleViolation()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");

        var exception = Assert.Throws<DomainRuleViolationException>(() => seat.Release());

        Assert.Equal(InventoryErrors.SeatNotReserved(seat.Id).Code, exception.Code);
    }

    [Fact]
    public void Release_WhenSold_ThrowsDomainRuleViolation()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        seat.Reserve();
        seat.Sell();

        var exception = Assert.Throws<DomainRuleViolationException>(() => seat.Release());

        Assert.Equal(InventoryErrors.SeatNotReserved(seat.Id).Code, exception.Code);
    }

    [Fact]
    public void Sell_WhenReserved_TransitionsToSold()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        seat.Reserve();

        seat.Sell();

        Assert.Equal(SeatAvailability.Sold, seat.Availability);
    }

    [Fact]
    public void Sell_WhenNotReserved_ThrowsDomainRuleViolation()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");

        var exception = Assert.Throws<DomainRuleViolationException>(() => seat.Sell());

        Assert.Equal(InventoryErrors.SeatNotReserved(seat.Id).Code, exception.Code);
    }

    [Fact]
    public void Sell_WhenAlreadySold_ThrowsDomainRuleViolation()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        seat.Reserve();
        seat.Sell();

        var exception = Assert.Throws<DomainRuleViolationException>(() => seat.Sell());

        Assert.Equal(InventoryErrors.SeatNotReserved(seat.Id).Code, exception.Code);
    }

    [Fact]
    public void IsAvailable_WhenAvailable_ReturnsTrue()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        Assert.True(seat.IsAvailable);
    }

    [Fact]
    public void IsAvailable_WhenReserved_ReturnsFalse()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        seat.Reserve();
        Assert.False(seat.IsAvailable);
    }

    [Fact]
    public void IsReserved_WhenAvailable_ReturnsFalse()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        Assert.False(seat.IsReserved);
    }

    [Fact]
    public void IsReserved_WhenReserved_ReturnsTrue()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");
        seat.Reserve();
        Assert.True(seat.IsReserved);
    }

    [Fact]
    public void Create_WhenCalled_InitializesAsAvailable()
    {
        var seat = InventorySeat.Create(Guid.CreateVersion7(), "Main", "A", "1");

        Assert.Equal(SeatAvailability.Available, seat.Availability);
        Assert.True(seat.IsAvailable);
    }
}

public sealed class GeneralAdmissionPoolReservationTests
{
    [Fact]
    public void Reserve_WhenQuantityAvailable_DecreasesAvailableCount()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);
        Assert.Equal(100, pool.AvailableCount);

        pool.Reserve(new Quantity(30));

        Assert.Equal(70, pool.AvailableCount);
        Assert.Equal(30, pool.ReservedCount);
    }

    [Fact]
    public void Reserve_WhenQuantityExceedsAvailable_ThrowsDomainRuleViolation()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            pool.Reserve(new Quantity(101)));

        Assert.Equal(InventoryErrors.NotEnoughGeneralAdmissionPoolCapacity(pool.Id, 101, 100).Code, exception.Code);
    }

    [Fact]
    public void Reserve_WithZeroQuantity_ThrowsArgumentException()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);

        var exception = Assert.Throws<ArgumentException>(() =>
            pool.Reserve(new Quantity(0)));

        // Quantity constructor itself rejects 0
        Assert.NotNull(exception);
    }

    [Fact]
    public void Reserve_MultipleReservations_StackCorrectly()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);

        pool.Reserve(new Quantity(20));
        pool.Reserve(new Quantity(30));
        pool.Reserve(new Quantity(25));

        Assert.Equal(75, pool.ReservedCount);
        Assert.Equal(25, pool.AvailableCount);
    }

    [Fact]
    public void Release_WhenReservedQuantityAvailable_RestoresAvailableCount()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);
        pool.Reserve(new Quantity(40));

        pool.Release(new Quantity(15));

        Assert.Equal(25, pool.ReservedCount);
        Assert.Equal(75, pool.AvailableCount);
    }

    [Fact]
    public void Release_WhenQuantityExceedsReserved_ThrowsDomainRuleViolation()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);
        pool.Reserve(new Quantity(20));

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            pool.Release(new Quantity(30)));

        Assert.Equal(InventoryErrors.NotEnoughReservedGeneralAdmissionPoolQuantity(pool.Id, 30, 20).Code, exception.Code);
    }

    [Fact]
    public void Release_WhenNoReservationsExist_ThrowsDomainRuleViolation()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            pool.Release(new Quantity(1)));

        Assert.Equal(InventoryErrors.NotEnoughReservedGeneralAdmissionPoolQuantity(pool.Id, 1, 0).Code, exception.Code);
    }

    [Fact]
    public void Sell_WhenSoldQuantityAvailable_DecreasesSoldCount()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);
        pool.Reserve(new Quantity(50));

        pool.Sell(new Quantity(30));

        Assert.Equal(20, pool.ReservedCount); // 50 - 30 sold
        Assert.Equal(30, pool.SoldCount);
        Assert.Equal(50, pool.AvailableCount); // 100 - 20 - 30
    }

    [Fact]
    public void Sell_WhenQuantityExceedsReserved_ThrowsDomainRuleViolation()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);
        pool.Reserve(new Quantity(30));

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            pool.Sell(new Quantity(40)));

        // Sell uses NotEnoughGeneralAdmissionPoolCapacity for consistency
        Assert.Equal(InventoryErrors.NotEnoughGeneralAdmissionPoolCapacity(pool.Id, 40, 30).Code, exception.Code);
    }

    [Fact]
    public void Sell_WhenNoReservationsExist_ThrowsDomainRuleViolation()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            pool.Sell(new Quantity(5)));

        Assert.Equal(InventoryErrors.NotEnoughGeneralAdmissionPoolCapacity(pool.Id, 5, 0).Code, exception.Code);
    }

    [Fact]
    public void AvailableCount_ReflectsReservedAndSold()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);

        Assert.Equal(100, pool.AvailableCount);

        pool.Reserve(new Quantity(30));
        Assert.Equal(70, pool.AvailableCount);

        pool.Reserve(new Quantity(20));
        Assert.Equal(50, pool.AvailableCount);

        pool.Sell(new Quantity(15));
        Assert.Equal(50, pool.AvailableCount); // 100 - (50-15) reserved - 15 sold = 100 - 35 - 15
    }

    [Fact]
    public void ReserveAndRelease_RestoresFullCapacity()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);

        pool.Reserve(new Quantity(50));
        Assert.Equal(50, pool.AvailableCount);

        pool.Release(new Quantity(50));
        Assert.Equal(100, pool.AvailableCount);
        Assert.Equal(0, pool.ReservedCount);
    }

    [Fact]
    public void Create_WhenCalled_InitializesWithZeroReservedAndSold()
    {
        var pool = GeneralAdmissionPool.Create(Guid.CreateVersion7(), "Floor", 100);

        Assert.Equal(100, pool.Capacity.Value);
        Assert.Equal(0, pool.ReservedCount);
        Assert.Equal(0, pool.SoldCount);
        Assert.Equal(100, pool.AvailableCount);
    }
}
