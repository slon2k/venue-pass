using System.Net;
using System.Net.Http.Json;
using VenuePass.Modules.Events.IntegrationTests.Infrastructure;
using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Features.CreateVenue;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class CreateVenueEndpointTests
{
    private readonly HttpClient _client;

    public CreateVenueEndpointTests(EventsIntegrationTestFixture fixture)
    {
        _client = fixture.CreateAdminClient();
    }

    [Fact]
    public async Task CreateVenue_WhenRequestIsValid_ReturnsCreatedWithNormalizedName()
    {
        var uniqueName = $"Main Hall {Guid.NewGuid()}";
        CreateVenueRequest request = new(
            Name: $"  {uniqueName}  ",
            Address: "123 Main St",
            City: "Seattle",
            Country: "US",
            Capacity: 250);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events/venues", request);

        CreateVenueResponse? body = await response.Content.ReadFromJsonAsync<CreateVenueResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotNull(body);
        Assert.Equal(uniqueName, body.Name);
        Assert.Equal("Seattle", body.City);
        Assert.Equal(250, body.Capacity);
    }

    [Fact]
    public async Task CreateVenue_WhenDuplicateNameInSameCity_ReturnsConflict()
    {
        var uniqueName = $"Grand Arena {Guid.NewGuid()}";
        CreateVenueRequest firstRequest = new(
            Name: uniqueName,
            Address: "1 First St",
            City: "Seattle",
            Country: "US",
            Capacity: 500);

        CreateVenueRequest duplicateRequest = new(
            Name: uniqueName,
            Address: "99 Other St",
            City: "Seattle",
            Country: "US",
            Capacity: 550);

        HttpResponseMessage firstResponse = await _client.PostAsJsonAsync("/events/venues", firstRequest);
        HttpResponseMessage duplicateResponse = await _client.PostAsJsonAsync("/events/venues", duplicateRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task CreateVenue_WhenCapacityIsInvalid_ReturnsBadRequest()
    {
        var uniqueName = $"Tiny Room {Guid.NewGuid()}";
        CreateVenueRequest request = new(
            Name: uniqueName,
            Address: "5 Pine St",
            City: "Seattle",
            Country: "US",
            Capacity: 0);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events/venues", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
}
