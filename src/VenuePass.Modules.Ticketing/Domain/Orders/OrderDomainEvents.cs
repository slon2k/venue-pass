using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Ticketing.Domain.Reservations;

namespace VenuePass.Modules.Ticketing.Domain.Orders;

public record OrderCreatedDomainEvent(OrderId OrderId, ReservationId ReservationId) : DomainEvent;