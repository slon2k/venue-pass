using System.Net;
using System.Net.Http.Json;

using VenuePass.Modules.Events.IntegrationTests.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Features.GetEvent;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class GetEventEndpointTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _client;
    private readonly HttpClient _adminClient;

    public GetEventEndpointTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateEventManagerClient();
        _adminClient = fixture.CreateAdminClient();
    }

    [Fact]
    public async Task GetEvent_WhenEventExists_ReturnsFullEventWithManifestSnapshot()
    {
        Guid venueId = await CreateVenueAsync();
        Guid templateId = await CreateManifestTemplateAsync(venueId);

        var managerId = Guid.NewGuid().ToString();
        var managerClient = _fixture.CreateEventManagerClient(managerId);

        var eventName = $"Winter Gala {Guid.NewGuid()}";
        DateTimeOffset eventDate = DateTimeOffset.UtcNow.AddMonths(6);

        Guid eventId = await CreateEventAsync(venueId, templateId, eventName, eventDate, managerClient);

        HttpResponseMessage response = await _client.GetAsync($"/events/{eventId}");
        GetEventResponse? body = await response.Content.ReadFromJsonAsync<GetEventResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);

        Assert.Equal(eventId, body.EventId);
        Assert.Equal(venueId, body.VenueId);
        Assert.Equal(eventName, body.Name);
        Assert.Equal("Draft", body.State);
        Assert.Null(body.Description);
        Assert.Equal(Guid.Parse(managerId), body.AssignedManagerId);

        Assert.NotNull(body.Manifest);
        Assert.NotEqual(Guid.Empty, body.Manifest.ManifestId);
        Assert.Equal(body.ManifestId, body.Manifest.ManifestId);

        Assert.Single(body.Manifest.Sections);
        Assert.Equal("Floor", body.Manifest.Sections[0].Name);

        Assert.Single(body.Manifest.Sections[0].Rows);
        Assert.Equal("A", body.Manifest.Sections[0].Rows[0].Label);

        Assert.Equal(2, body.Manifest.Sections[0].Rows[0].Seats.Count);
        Assert.Contains(body.Manifest.Sections[0].Rows[0].Seats, s => s.Label == "1");
        Assert.Contains(body.Manifest.Sections[0].Rows[0].Seats, s => s.Label == "2");

        Assert.Single(body.Manifest.GeneralAdmissionAreas);
        Assert.Equal("GA East", body.Manifest.GeneralAdmissionAreas[0].Name);
        Assert.Equal(300, body.Manifest.GeneralAdmissionAreas[0].Capacity);
    }

    [Fact]
    public async Task GetEvent_WhenEventDoesNotExist_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync($"/events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateVenueAsync()
    {
        CreateVenueRequest request = new(
            Name: $"Venue {Guid.NewGuid()}",
            Address: "123 Main St",
            City: "Seattle",
            Country: "US",
            Capacity: 500);

        HttpResponseMessage response = await _adminClient.PostAsJsonAsync("/events/venues", request);
        CreateVenueResponse? body = await response.Content.ReadFromJsonAsync<CreateVenueResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);

        return body.VenueId;
    }

    private async Task<Guid> CreateManifestTemplateAsync(Guid venueId)
    {
        CreateManifestTemplateRequest request = new(
            Name: $"Template {Guid.NewGuid()}",
            Description: "Concert layout",
            VenueId: venueId,
            Sections:
            [
                new CreateManifestTemplateSectionRequest(
                    Name: "Floor",
                    Rows:
                    [
                        new CreateManifestTemplateRowRequest(
                            Label: "A",
                            Seats:
                            [
                                new CreateManifestTemplateSeatRequest("1"),
                                new CreateManifestTemplateSeatRequest("2")
                            ])
                    ])
            ],
            GeneralAdmissionAreas:
            [
                new CreateManifestTemplateGeneralAdmissionAreaRequest(
                    Name: "GA East",
                    Capacity: 300)
            ]);

        HttpResponseMessage response = await _adminClient.PostAsJsonAsync("/events/manifest-templates", request);
        CreateManifestTemplateResponse? body = await response.Content.ReadFromJsonAsync<CreateManifestTemplateResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);

        return body.ManifestTemplateId;
    }

    private async Task<Guid> CreateEventAsync(
        Guid venueId,
        Guid templateId,
        string name,
        DateTimeOffset eventDate,
        HttpClient? client = null)
    {
        client ??= _client;
        CreateEventRequest request = new(
            VenueId: venueId,
            ManifestTemplateId: templateId,
            Name: name,
            EventDate: eventDate,
            Description: null);

        HttpResponseMessage response = await client.PostAsJsonAsync("/events", request);
        CreateEventResponse? body = await response.Content.ReadFromJsonAsync<CreateEventResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);

        return body.EventId;
    }

    private sealed record CreateEventRequest(
        Guid VenueId,
        Guid ManifestTemplateId,
        string Name,
        DateTimeOffset EventDate,
        string? Description);

    private sealed record CreateEventResponse(Guid EventId, Guid ManifestId, Guid AssignedManagerId);

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

    private sealed record CreateManifestTemplateRequest(
        string Name,
        string? Description,
        Guid VenueId,
        IReadOnlyList<CreateManifestTemplateSectionRequest> Sections,
        IReadOnlyList<CreateManifestTemplateGeneralAdmissionAreaRequest> GeneralAdmissionAreas);

    private sealed record CreateManifestTemplateSectionRequest(
        string Name,
        IReadOnlyList<CreateManifestTemplateRowRequest> Rows);

    private sealed record CreateManifestTemplateRowRequest(
        string Label,
        IReadOnlyList<CreateManifestTemplateSeatRequest> Seats);

    private sealed record CreateManifestTemplateSeatRequest(string Label);

    private sealed record CreateManifestTemplateGeneralAdmissionAreaRequest(string Name, int Capacity);

    private sealed record CreateManifestTemplateResponse(Guid ManifestTemplateId);

    private sealed record GetEventResponse(
        Guid EventId,
        Guid VenueId,
        Guid ManifestId,
        string Name,
        DateTimeOffset EventDate,
        string? Description,
        string State,
        Guid AssignedManagerId,
        GetEventManifestResponse? Manifest);

    private sealed record GetEventManifestResponse(
        Guid ManifestId,
        string Name,
        IReadOnlyList<GetEventSectionResponse> Sections,
        IReadOnlyList<GetEventGeneralAdmissionAreaResponse> GeneralAdmissionAreas);

    private sealed record GetEventSectionResponse(
        string Name,
        IReadOnlyList<GetEventRowResponse> Rows);

    private sealed record GetEventRowResponse(
        string Label,
        IReadOnlyList<GetEventSeatResponse> Seats);

    private sealed record GetEventSeatResponse(string Label);

    private sealed record GetEventGeneralAdmissionAreaResponse(string Name, int Capacity);
}
