using System.Net;
using System.Net.Http.Json;

using VenuePass.IntegrationTests.Ticketing.Fixtures;
using VenuePass.IntegrationTests.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Ticketing.Inventory;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class GetInventoryStatusTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _managerClient;

    public GetInventoryStatusTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _managerClient = fixture.CreateEventManagerClient();
    }

    [Fact]
    public async Task GetInventoryStatus_WhenInventoryExists_Returns200WithSectionCounts()
    {
        // The seed helper uses: 1 section ("Floor"), 1 row ("A"), 2 seats, 1 GA area ("GA East").
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);

        HttpResponseMessage response = await _managerClient.GetAsync($"/events/{eventId}/inventory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetInventoryStatusResponse? body = await response.Content.ReadFromJsonAsync<GetInventoryStatusResponse>();
        Assert.NotNull(body);
        Assert.Equal(eventId, body!.EventId);
        Assert.NotEqual(Guid.Empty, body.InventoryId);
        Assert.Equal(2, body.TotalSeats);
        Assert.Equal(2, body.AvailableSeats);

        Assert.NotNull(body.Sections);
        Assert.NotEmpty(body.Sections);

        SectionStatusResponse? floorSection = body.Sections.FirstOrDefault(s => s.Name == "Floor");
        Assert.NotNull(floorSection);
        Assert.Equal(2, floorSection!.TotalSeats);
        Assert.Equal(2, floorSection.AvailableSeats);
    }

    [Fact]
    public async Task GetInventoryStatus_WhenInventoryExists_ReturnsCorrectPoolData()
    {
        Guid eventId = await TicketingSeedHelpers.PublishEventAndSyncInventoryAsync(_fixture, _managerClient);

        HttpResponseMessage response = await _managerClient.GetAsync($"/events/{eventId}/inventory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetInventoryStatusResponse? body = await response.Content.ReadFromJsonAsync<GetInventoryStatusResponse>();
        Assert.NotNull(body);

        Assert.NotNull(body!.Pools);
        Assert.Single(body.Pools);

        PoolStatusResponse pool = body.Pools[0];
        Assert.Equal("GA East", pool.Name);
        Assert.Equal(300, pool.TotalCapacity);
        Assert.Equal(300, pool.AvailableCount);
    }

    [Fact]
    public async Task GetInventoryStatus_WhenEventNotPublished_Returns404()
    {
        Guid unpublishedEventId = Guid.NewGuid();

        HttpResponseMessage response = await _managerClient.GetAsync($"/events/{unpublishedEventId}/inventory");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetInventoryStatus_WhenUnauthenticated_Returns401()
    {
        HttpClient unauthenticated = _fixture.Client;

        HttpResponseMessage response = await unauthenticated.GetAsync($"/events/{Guid.NewGuid()}/inventory");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record GetInventoryStatusResponse(
        Guid EventId,
        Guid InventoryId,
        int TotalSeats,
        int AvailableSeats,
        IReadOnlyList<SectionStatusResponse> Sections,
        IReadOnlyList<PoolStatusResponse> Pools);

    private sealed record SectionStatusResponse(string Name, int TotalSeats, int AvailableSeats);

    private sealed record PoolStatusResponse(string Name, int TotalCapacity, int AvailableCount);
}
