using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Inventories;
using VenuePass.Modules.Ticketing.Domain.Orders;
using VenuePass.Modules.Ticketing.Domain.PublishedEvents;

namespace VenuePass.Modules.Ticketing.Domain.Tickets
{
    public class Ticket : AggregateRoot<TicketId>
    {
        public PublishedEventReferenceId PublishedEventReferenceId { get; private set; }
        public OrderId OrderId { get; private set; }

        public OrderItemId OrderItemId { get; private set; }

        public TicketCode Code { get; private set; }

        public InventorySeatId? InventorySeatId { get; private set; }

        public GeneralAdmissionPoolId? GeneralAdmissionPoolId { get; private set; }

        public TicketStatus Status { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? CanceledAt { get; private set; }

        private Ticket() { }

        private Ticket(
            TicketId id,
            PublishedEventReferenceId publishedEventReferenceId,
            OrderId orderId,
            OrderItemId orderItemId,
            TicketCode code,
            InventorySeatId? inventorySeatId,
            GeneralAdmissionPoolId? generalAdmissionPoolId,
            DateTimeOffset createdAt) : base(id)
        {
            if (id.IsEmpty)
                throw new ArgumentException("Ticket ID cannot be empty.", nameof(id));

            if (publishedEventReferenceId.IsEmpty)
                throw new ArgumentException("Published Event Reference ID cannot be empty.", nameof(publishedEventReferenceId));

            if (orderId.IsEmpty)
                throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));

            if (orderItemId.IsEmpty)
                throw new ArgumentException("Order Item ID cannot be empty.", nameof(orderItemId));

            if (inventorySeatId.HasValue && inventorySeatId.Value.IsEmpty)
                throw new ArgumentException("Inventory Seat ID cannot be empty when provided.", nameof(inventorySeatId));

            if (generalAdmissionPoolId.HasValue && generalAdmissionPoolId.Value.IsEmpty)
                throw new ArgumentException("General Admission Pool ID cannot be empty when provided.", nameof(generalAdmissionPoolId));

            if (inventorySeatId.HasValue && generalAdmissionPoolId.HasValue)
                throw new DomainRuleViolationException(TicketErrors.InvalidTicketAssociation(inventorySeatId.Value, generalAdmissionPoolId.Value));

            if (!inventorySeatId.HasValue && !generalAdmissionPoolId.HasValue)
                throw new DomainRuleViolationException(TicketErrors.MissingTicketAssociation());

            if (createdAt == default)
                throw new ArgumentException("Creation time cannot be the default value.", nameof(createdAt));

            if (code.IsEmpty)
                throw new ArgumentException("Ticket code cannot be empty.", nameof(code));            

            PublishedEventReferenceId = publishedEventReferenceId;
            OrderId = orderId;
            OrderItemId = orderItemId;
            Code = code;
            InventorySeatId = inventorySeatId;
            GeneralAdmissionPoolId = generalAdmissionPoolId;
            Status = TicketStatus.Issued;
            CreatedAt = createdAt;
        }

        internal static Ticket CreateForInventorySeat(
            PublishedEventReferenceId publishedEventReferenceId,
            OrderId orderId,
            OrderItemId orderItemId,
            TicketCode code,
            InventorySeatId inventorySeatId,
            DateTimeOffset now)
        {
            return new Ticket(
                id: TicketId.Create(),
                publishedEventReferenceId: publishedEventReferenceId,
                orderId: orderId,
                orderItemId: orderItemId,
                code: code,
                inventorySeatId: inventorySeatId,
                generalAdmissionPoolId: null,
                createdAt: now
            );
        }

        internal static Ticket CreateForGeneralAdmissionPool(
            PublishedEventReferenceId publishedEventReferenceId,
            OrderId orderId,
            OrderItemId orderItemId,
            TicketCode code,
            GeneralAdmissionPoolId generalAdmissionPoolId,
            DateTimeOffset now)
        {
            return new Ticket(
                id: TicketId.Create(),
                publishedEventReferenceId: publishedEventReferenceId,
                orderId: orderId,
                orderItemId: orderItemId,
                code: code,
                inventorySeatId: null,
                generalAdmissionPoolId: generalAdmissionPoolId,
                createdAt: now
            );
        }

        public bool Cancel(DateTimeOffset canceledAt)
        {
            if (Status == TicketStatus.Canceled)
            {
                return false;
            };

            Status = TicketStatus.Canceled;
            CanceledAt = canceledAt;
            return true;
        }
    }

    public readonly record struct TicketId(Guid Value)
    {
        public static TicketId Create() => new(Guid.CreateVersion7());
        public bool IsEmpty => Value == Guid.Empty;   
        public static implicit operator Guid(TicketId id) => id.Value;
        public override string ToString() => Value.ToString();
    }

    public readonly record struct TicketCode
    {
        private const string CrockfordBase32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        public const int Length = 16;
        public string Value { get; }

        public static bool TryCreate(string value, out TicketCode ticketCode)
        {
            ticketCode = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim().Replace("-", "").ToUpperInvariant();

            if (value.Length != Length)
                return false;

            foreach (var c in value)
            {
                if (!CrockfordBase32.Contains(c))
                    return false;
            }

            ticketCode = new TicketCode(value);
            return true;
        }

        public TicketCode(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

            value = value.Trim().Replace("-", "").ToUpperInvariant();

            if (value.Length != Length)
            {
                throw new ArgumentException($"Ticket code must be {Length} characters long.", nameof(value));
            }

            foreach (var c in value)            
            {
                if (!CrockfordBase32.Contains(c))
                {
                    throw new ArgumentException(
                        $"Ticket code contains invalid character '{c}'. Only Crockford's Base32 characters are allowed.",
                        nameof(value));
                }
            }

            Value = value;
        }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public enum TicketStatus
    {
        Issued = 1,

        Canceled = 2,
    }
}