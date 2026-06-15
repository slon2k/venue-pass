using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Common;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Offers;
using VenuePass.Modules.Ticketing.Domain.Reservations;

namespace VenuePass.Modules.Ticketing.Domain.Orders;

public class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderItem> _items = [];

    public ReservationId ReservationId { get; private set; }

    public OfferId OfferId { get; private set; }

    public InventoryId InventoryId { get; private set; }

    public string BuyerName { get; private set; } = null!;

    public string BuyerEmail { get; private set; } = null!;

    public Currency Currency { get; private set; } = null!;

    public Amount Total { get; private set; } = new Amount(0);

    public OrderStatus Status { get; private set; } = OrderStatus.Completed;

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    private Order(
        OrderId id,
        ReservationId reservationId,
        OfferId offerId,
        InventoryId inventoryId,
        string buyerName,
        string buyerEmail,
        Currency currency,
        DateTimeOffset createdAt,
        IReadOnlyList<OrderItem> items) : base(id)
    {
        if (id.IsEmpty)
            throw new ArgumentException("Order ID cannot be empty.", nameof(id));

        if (reservationId.IsEmpty)
            throw new ArgumentException("Reservation ID cannot be empty.", nameof(reservationId));

        if (offerId.IsEmpty)
            throw new ArgumentException("Offer ID cannot be empty.", nameof(offerId));

        if (inventoryId.IsEmpty)
            throw new ArgumentException("Inventory ID cannot be empty.", nameof(inventoryId));

        ArgumentException.ThrowIfNullOrWhiteSpace(buyerName, nameof(buyerName));
        ArgumentException.ThrowIfNullOrWhiteSpace(buyerEmail, nameof(buyerEmail));

        if (items.Count == 0)
            throw new ArgumentException("Order must contain at least one item.", nameof(items));

        ReservationId = reservationId;
        OfferId = offerId;
        InventoryId = inventoryId;
        BuyerName = buyerName;
        BuyerEmail = buyerEmail;
        Currency = currency;
        Status = OrderStatus.Completed;
        CreatedAt = createdAt;
        _items.AddRange(items);
        Total = new Amount(_items.Sum(i => i.Total.Value));
    }

    public static Order CreateFromReservation(Reservation reservation, string buyerName, string buyerEmail, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentException.ThrowIfNullOrWhiteSpace(buyerName, nameof(buyerName));
        ArgumentException.ThrowIfNullOrWhiteSpace(buyerEmail, nameof(buyerEmail));

        if (!reservation.IsActive(now))
        {
            throw new DomainRuleViolationException(
                OrderErrors.ReservationNotActive(reservation.Id));
        }

        var items = reservation.Items
            .Select(OrderItem.CreateFromReservationItem)
            .ToList();
        
        var order = new Order(
            id: OrderId.Create(),
            reservationId: reservation.Id,
            offerId: reservation.OfferId,
            inventoryId: reservation.InventoryId,
            buyerName: buyerName,
            buyerEmail: buyerEmail,
            currency: reservation.Currency,
            createdAt: now,
            items: items);

        order.AddDomainEvent(new OrderCreatedDomainEvent(order.Id, reservation.Id));

        return order;
    }
}

public class OrderItem : Entity<OrderItemId>
{
    public OrderItemType Type { get; private set; }

    public PriceZoneId PriceZoneId { get; private set; }

    public InventorySeatId? InventorySeatId { get; private set; }

    public GeneralAdmissionPoolId? GeneralAdmissionPoolId { get; private set; }

    public Amount UnitPrice { get; private set; }

    public Quantity Quantity { get; private set; }

    public Amount Total { get; private set; }

    private OrderItem() { }

    private OrderItem(
        OrderItemId id,
        OrderItemType type,
        PriceZoneId priceZoneId,
        InventorySeatId? inventorySeatId,
        GeneralAdmissionPoolId? generalAdmissionPoolId,
        Amount unitPrice,
        Quantity quantity) : base(id)
    {
        if (id.IsEmpty)
            throw new ArgumentException("Order item ID cannot be empty.", nameof(id));

        if (priceZoneId.IsEmpty)
            throw new ArgumentException("Price zone ID cannot be empty.", nameof(priceZoneId));

        if (type == OrderItemType.Seat)
        {
            if (inventorySeatId is null || inventorySeatId.Value.IsEmpty)
                throw new ArgumentException("Inventory seat ID is required for seat order item.");

            if (generalAdmissionPoolId is not null)
                throw new ArgumentException("Seat order item cannot have a general admission pool ID.");

            if (quantity.Value != 1)
                throw new ArgumentException("Seat order item quantity must be 1.", nameof(quantity));
        }

        if (type == OrderItemType.GeneralAdmissionPool)
        {
            if (generalAdmissionPoolId is null || generalAdmissionPoolId.Value.IsEmpty)
                throw new ArgumentException("General admission pool ID is required for GA order item.");

            if (inventorySeatId is not null)
                throw new ArgumentException("GA order item cannot have an inventory seat ID.");

            if (quantity.Value <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        Type = type;
        PriceZoneId = priceZoneId;
        InventorySeatId = inventorySeatId;
        GeneralAdmissionPoolId = generalAdmissionPoolId;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Total = new Amount(unitPrice.Value * quantity.Value);
    }

    internal static OrderItem CreateFromReservationItem(ReservationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var type = item.Type == ReservationItemType.Seat
            ? OrderItemType.Seat
            : OrderItemType.GeneralAdmissionPool;

        return new OrderItem(
            id: OrderItemId.Create(),
            type: type,
            priceZoneId: item.PriceZoneId,
            inventorySeatId: item.InventorySeatId,
            generalAdmissionPoolId: item.GeneralAdmissionPoolId,
            unitPrice: item.UnitPrice,
            quantity: item.Quantity);
    }
}

public readonly record struct OrderId(Guid Value)
{
    public static OrderId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(OrderId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}

public readonly record struct OrderItemId(Guid Value)
{
    public static OrderItemId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(OrderItemId id) => id.Value;
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}

public enum OrderStatus
{
    Completed = 1
}

public enum OrderItemType
{
    Seat = 1,
    GeneralAdmissionPool = 2
}
