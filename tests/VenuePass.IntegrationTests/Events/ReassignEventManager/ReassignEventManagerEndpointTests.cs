using System.Net;
using System.Net.Http.Json;

using VenuePass.IntegrationTests.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Events.ReassignEventManager;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class ReassignEventManagerEndpointTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _managerClient;
    private readonly HttpClient _unauthenticatedClient;

    public ReassignEventManagerEndpointTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _adminClient = fixture.CreateAdminClient();
        _managerClient = fixture.CreateEventManagerClient();
        _unauthenticatedClient = fixture.Client;
    }

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ReassignEventManager_WhenAdminAndEventExists_Returns204()
    {
        Guid eventId = await CreateEventAsync();
        var newManagerId = Guid.NewGuid();

        HttpResponseMessage response = await _adminClient.PostAsJsonAsync(
            $"/events/{eventId}/reassign-manager",
            new ReassignEventManagerRequest(NewManagerId: newManagerId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ReassignEventManager_WhenAdminAndEventExists_PersistsNewManagerId()
    {
        var originalManagerId = Guid.NewGuid().ToString();
        Guid eventId = await CreateEventAsync(managerId: originalManagerId);
        var newManagerId = Guid.NewGuid();

        await _adminClient.PostAsJsonAsync(
            $"/events/{eventId}/reassign-manager",
            new ReassignEventManagerRequest(NewManagerId: newManagerId));

        HttpResponseMessage getResponse = await _adminClient.GetAsync($"/events/{eventId}");
        GetEventResponse? body = await getResponse.Content.ReadFromJsonAsync<GetEventResponse>();

        Assert.NotNull(body);
        Assert.Equal(newManagerId, body.AssignedManagerId);
    }

    // ── Not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReassignEventManager_WhenEventDoesNotExist_Returns404()
    {
        HttpResponseMessage response = await _adminClient.PostAsJsonAsync(
            $"/events/{Guid.NewGuid()}/reassign-manager",
            new ReassignEventManagerRequest(NewManagerId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReassignEventManager_WhenNewManagerIdIsEmpty_Returns400()
    {
        Guid eventId = await CreateEventAsync();

        HttpResponseMessage response = await _adminClient.PostAsJsonAsync(
            $"/events/{eventId}/reassign-manager",
            new ReassignEventManagerRequest(NewManagerId: Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Auth enforcement ──────────────────────────────────────────────────────

    [Fact]
    public async Task ReassignEventManager_WhenUnauthenticated_Returns401()
    {
        HttpResponseMessage response = await _unauthenticatedClient.PostAsJsonAsync(
            $"/events/{Guid.NewGuid()}/reassign-manager",
            new ReassignEventManagerRequest(NewManagerId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReassignEventManager_WhenCallerIsEventManager_Returns403()
    {
        HttpResponseMessage response = await _managerClient.PostAsJsonAsync(
            $"/events/{Guid.NewGuid()}/reassign-manager",
            new ReassignEventManagerRequest(NewManagerId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> CreateEventAsync(string? managerId = null)
    {
        var managerClient = managerId is null
            ? _managerClient
            : _fixture.CreateEventManagerClient(managerId);

        Guid venueId = await CreateVenueAsync();
        Guid templateId = await CreateManifestTemplateAsync(venueId);

        CreateEventRequest request = new(
            VenueId: venueId,
            ManifestTemplateId: templateId,
            Name: $"Concert {Guid.NewGuid()}",
            EventDate: DateTimeOffset.UtcNow.AddMonths(3),
            Description: null);

        HttpResponseMessage response = await managerClient.PostAsJsonAsync("/events", request);
        CreateEventResponse? body = await response.Content.ReadFromJsonAsync<CreateEventResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);

        return body.EventId;
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

    private sealed record ReassignEventManagerRequest(Guid NewManagerId);

    private sealed record GetEventResponse(
        Guid EventId,
        Guid VenueId,
        Guid ManifestId,
        string Name,
        DateTimeOffset EventDate,
        string? Description,
        string State,
        Guid AssignedManagerId);

    private sealed record CreateEventRequest(
        Guid VenueId,
        Guid ManifestTemplateId,
        string Name,
        DateTimeOffset EventDate,
        string? Description);

    private sealed record CreateEventResponse(Guid EventId, Guid ManifestId);

    private sealed record CreateVenueRequest(
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    private sealed record CreateVenueResponse(Guid VenueId);

    private sealed record CreateManifestTemplateRequest(
        string Name,
        string? Description,
        Guid VenueId,
        List<CreateManifestTemplateSectionRequest> Sections,
        List<CreateManifestTemplateGeneralAdmissionAreaRequest> GeneralAdmissionAreas);

    private sealed record CreateManifestTemplateSectionRequest(
        string Name,
        List<CreateManifestTemplateRowRequest> Rows);

    private sealed record CreateManifestTemplateRowRequest(
        string Label,
        List<CreateManifestTemplateSeatRequest> Seats);

    private sealed record CreateManifestTemplateSeatRequest(string Label);

    private sealed record CreateManifestTemplateGeneralAdmissionAreaRequest(
        string Name,
        int Capacity);

    private sealed record CreateManifestTemplateResponse(Guid ManifestTemplateId);
}
