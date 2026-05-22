using System.Net;
using System.Net.Http.Json;
using VenuePass.Modules.Events.IntegrationTests.Infrastructure;
using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Features.GetVenue;

public sealed class GetVenueEndpointTests : IClassFixture<EventsIntegrationTestFixture>
{
    private readonly HttpClient _client;
    private readonly EventsIntegrationTestFixture _fixture;

    public GetVenueEndpointTests(EventsIntegrationTestFixture fixture)
    {
        _client = fixture.Client;
        _fixture = fixture;
    }

    [Fact]
    public async Task GetVenue_WhenVenueExists_ReturnsOkWithVenueDetails()
    {
        var uniqueName = $"Test Venue {Guid.NewGuid()}";
        CreateVenueRequest createRequest = new(
            Name: uniqueName,
            Address: "456 Oak St",
            City: "Portland",
            Country: "US",
            Capacity: 300);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/events/venues", createRequest);
        CreateVenueResponse? createBody = await createResponse.Content.ReadFromJsonAsync<CreateVenueResponse>();

        Assert.NotNull(createBody);
        Guid venueId = createBody.VenueId;

        HttpResponseMessage getResponse = await _client.GetAsync($"/events/venues/{venueId}");
        GetVenueResponse? getBody = await getResponse.Content.ReadFromJsonAsync<GetVenueResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(getBody);
        Assert.Equal(venueId, getBody.VenueId);
        Assert.Equal(uniqueName, getBody.Name);
        Assert.Equal("456 Oak St", getBody.Address);
        Assert.Equal("Portland", getBody.City);
        Assert.Equal("US", getBody.Country);
        Assert.Equal(300, getBody.Capacity);
    }

    [Fact]
    public async Task GetVenue_WhenVenueDoesNotExist_ReturnsNotFound()
    {
        Guid nonExistentId = Guid.NewGuid();

        HttpResponseMessage response = await _client.GetAsync($"/events/venues/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetVenue_WhenMultipleVenuesExist_ReturnsCorrectVenue()
    {
        var uniqueName1 = $"Arena {Guid.NewGuid()}";
        var uniqueName2 = $"Stadium {Guid.NewGuid()}";

        CreateVenueRequest request1 = new(
            Name: uniqueName1,
            Address: "111 First Ave",
            City: "Boston",
            Country: "US",
            Capacity: 1000);

        CreateVenueRequest request2 = new(
            Name: uniqueName2,
            Address: "222 Second Ave",
            City: "Boston",
            Country: "US",
            Capacity: 2000);

        HttpResponseMessage response1 = await _client.PostAsJsonAsync("/events/venues", request1);
        HttpResponseMessage response2 = await _client.PostAsJsonAsync("/events/venues", request2);

        CreateVenueResponse? body1 = await response1.Content.ReadFromJsonAsync<CreateVenueResponse>();
        CreateVenueResponse? body2 = await response2.Content.ReadFromJsonAsync<CreateVenueResponse>();

        Assert.NotNull(body1);
        Assert.NotNull(body2);

        HttpResponseMessage getResponse1 = await _client.GetAsync($"/events/venues/{body1.VenueId}");
        GetVenueResponse? getBody1 = await getResponse1.Content.ReadFromJsonAsync<GetVenueResponse>();

        HttpResponseMessage getResponse2 = await _client.GetAsync($"/events/venues/{body2.VenueId}");
        GetVenueResponse? getBody2 = await getResponse2.Content.ReadFromJsonAsync<GetVenueResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);

        Assert.NotNull(getBody1);
        Assert.NotNull(getBody2);

        Assert.Equal(uniqueName1, getBody1.Name);
        Assert.Equal(1000, getBody1.Capacity);

        Assert.Equal(uniqueName2, getBody2.Name);
        Assert.Equal(2000, getBody2.Capacity);
    }

    private sealed record CreateVenueRequest(
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    private sealed record CreateVenueResponse(
        Guid VenueId,
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    private sealed record GetVenueResponse(
        Guid VenueId,
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);
}
