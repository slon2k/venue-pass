using System.Net;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.IntegrationTests.Ticketing.Fixtures;
using VenuePass.Modules.Ticketing.Contracts;
using VenuePass.Modules.Ticketing.Domain.Tickets;
using VenuePass.Modules.Ticketing.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.Tickets;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class CancelTicketTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _unauthenticatedClient;
    private readonly HttpClient _customerClient;

    public CancelTicketTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
        _adminClient = fixture.CreateAdminClient();
        _unauthenticatedClient = fixture.Client;
        _customerClient = fixture.CreateAuthenticatedCustomerClient();
    }

    [Fact]
    public async Task CancelTicket_WhenIssuedTicket_Returns204_UpdatesState_AndWritesOutbox()
    {
        var ticketId = await CreateIssuedTicketAsync();

        HttpResponseMessage response = await _managerClient.DeleteAsync($"/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var ticket = await db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == new TicketId(ticketId));

        Assert.NotNull(ticket);
        Assert.Equal(TicketStatus.Canceled, ticket!.Status);
        Assert.NotNull(ticket.CanceledAt);

        var canceledMessages = await LoadTicketCanceledMessagesAsync(db, ticketId);
        Assert.Single(canceledMessages);

        var message = canceledMessages[0];
        Assert.Equal(ticket.PublishedEventReferenceId.Value, message.EventId);
        Assert.Equal(ticket.Code.Value, message.TicketCode);
        Assert.Equal(ticket.CanceledAt!.Value, message.OccurredOn);
    }

    [Fact]
    public async Task CancelTicket_WhenAlreadyCanceled_Returns204_AndDoesNotWriteDuplicateOutbox()
    {
        var ticketId = await CreateIssuedTicketAsync();

        HttpResponseMessage firstResponse = await _managerClient.DeleteAsync($"/tickets/{ticketId}");
        HttpResponseMessage secondResponse = await _managerClient.DeleteAsync($"/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

        var ticket = await db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == new TicketId(ticketId));

        Assert.NotNull(ticket);
        Assert.Equal(TicketStatus.Canceled, ticket!.Status);
        Assert.NotNull(ticket.CanceledAt);

        var canceledMessages = await LoadTicketCanceledMessagesAsync(db, ticketId);
        Assert.Single(canceledMessages);
    }

    [Fact]
    public async Task CancelTicket_WhenTicketDoesNotExist_Returns404()
    {
        HttpResponseMessage response = await _managerClient.DeleteAsync($"/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelTicket_WhenCallerIsNotEventManager_Returns403()
    {
        HttpResponseMessage response = await _adminClient.DeleteAsync($"/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelTicket_WhenUnauthenticated_Returns401()
    {
        HttpResponseMessage response = await _unauthenticatedClient.DeleteAsync($"/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> CreateIssuedTicketAsync()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);
        (List<Guid> seatIds, _) = await TicketingSeedHelpers.GetInventoryIdsAsync(_fixture, eventId);

        Guid offerId = await TicketingSeedHelpers.CreateOfferAsync(_managerClient, eventId);
        await TicketingSeedHelpers.ConfigurePriceZonesAsync(
            _managerClient,
            offerId,
            [new PriceZoneItem("Zone A", 25m, seatIds, [])]);
        await TicketingSeedHelpers.ActivateOfferAsync(_managerClient, offerId);

        Guid reservationId = await TicketingSeedHelpers.CreateReservationAsync(
            _customerClient,
            offerId,
            seatIds: [seatIds[0]]);

        Guid orderId = await TicketingSeedHelpers.CheckoutReservationAsync(_customerClient, reservationId);
        GetOrderSeedResponse order = await TicketingSeedHelpers.GetOrderAsync(_customerClient, orderId);

        Assert.Single(order.Tickets);
        return order.Tickets[0].TicketId;
    }

    private static async Task<List<TicketCanceledIntegrationEvent>> LoadTicketCanceledMessagesAsync(
        TicketingDbContext db,
        Guid ticketId)
    {
        var candidates = await db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.Type == typeof(TicketCanceledIntegrationEvent).AssemblyQualifiedName)
            .OrderByDescending(m => m.OccurredOn)
            .ToListAsync();

        var matching = new List<TicketCanceledIntegrationEvent>();

        foreach (var candidate in candidates)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<TicketCanceledIntegrationEvent>(candidate.Payload);
                if (payload is not null && payload.TicketId == ticketId)
                {
                    matching.Add(payload);
                }
            }
            catch (JsonException)
            {
                // Ignore malformed payloads from unrelated tests.
            }
        }

        return matching;
    }
}
