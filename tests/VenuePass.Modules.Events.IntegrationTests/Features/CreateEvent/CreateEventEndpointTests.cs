using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Infrastructure;
using VenuePass.Modules.Events.IntegrationTests.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Features.CreateEvent;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class CreateEventEndpointTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _client;
    private readonly HttpClient _adminClient;

    public CreateEventEndpointTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateEventManagerClient();
        _adminClient = fixture.CreateAdminClient();
    }

    [Fact]
    public async Task CreateEvent_WhenRequestIsValid_Returns201WithEventAndManifestIds()
    {
        var managerId = Guid.NewGuid().ToString();
        var managerClient = _fixture.CreateEventManagerClient(managerId);

        Guid venueId = await CreateVenueAsync();
        Guid templateId = await CreateManifestTemplateAsync(venueId);

        var eventName = $"Summer Concert {Guid.NewGuid()}";

        CreateEventRequest request = new(
            VenueId: venueId,
            ManifestTemplateId: templateId,
            Name: eventName,
            EventDate: DateTimeOffset.UtcNow.AddMonths(3),
            Description: "An outdoor summer concert.");

        HttpResponseMessage response = await managerClient.PostAsJsonAsync("/events", request);
        CreateEventResponse? body = await response.Content.ReadFromJsonAsync<CreateEventResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.EventId);
        Assert.NotEqual(Guid.Empty, body.ManifestId);

        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        Event? persistedEvent = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == new EventId(body.EventId));

        Assert.NotNull(persistedEvent);
        Assert.Equal(venueId, persistedEvent.VenueId.Value);
        Assert.Equal(eventName, persistedEvent.Name.Value);
        Assert.Equal(Guid.Parse(managerId), persistedEvent.AssignedManagerId.Value);

        Manifest? persistedManifest = await db.Manifests
            .AsNoTracking()
            .Include(m => m.Sections)
            .ThenInclude(s => s.Rows)
            .ThenInclude(r => r.Seats)
            .Include(m => m.GeneralAdmissionAreas)
            .FirstOrDefaultAsync(m => m.Id == new ManifestId(body.ManifestId));

        Assert.NotNull(persistedManifest);
        Assert.Equal(persistedEvent.Id, persistedManifest.EventId);

        Assert.Single(persistedManifest.Sections);
        Assert.Equal("Floor", persistedManifest.Sections[0].Name.Value);
        Assert.Single(persistedManifest.Sections[0].Rows);
        Assert.Equal("A", persistedManifest.Sections[0].Rows[0].Label.Value);
        Assert.Equal(2, persistedManifest.Sections[0].Rows[0].Seats.Count);

        Assert.Single(persistedManifest.GeneralAdmissionAreas);
        Assert.Equal("GA East", persistedManifest.GeneralAdmissionAreas[0].Name.Value);
        Assert.Equal(300, persistedManifest.GeneralAdmissionAreas[0].Capacity.Value);
    }

    [Fact]
    public async Task CreateEvent_WhenVenueDoesNotExist_ReturnsNotFound()
    {
        Guid venueId = await CreateVenueAsync();
        Guid templateId = await CreateManifestTemplateAsync(venueId);

        CreateEventRequest request = new(
            VenueId: Guid.NewGuid(),
            ManifestTemplateId: templateId,
            Name: $"Ghost Venue Event {Guid.NewGuid()}",
            EventDate: DateTimeOffset.UtcNow.AddMonths(1),
            Description: null);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WhenManifestTemplateDoesNotExist_ReturnsNotFound()
    {
        Guid venueId = await CreateVenueAsync();

        CreateEventRequest request = new(
            VenueId: venueId,
            ManifestTemplateId: Guid.NewGuid(),
            Name: $"Missing Template Event {Guid.NewGuid()}",
            EventDate: DateTimeOffset.UtcNow.AddMonths(1),
            Description: null);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WhenTemplateBelongsToDifferentVenue_ReturnsConflict()
    {
        Guid venue1Id = await CreateVenueAsync();
        Guid venue2Id = await CreateVenueAsync();
        Guid templateForVenue1 = await CreateManifestTemplateAsync(venue1Id);

        CreateEventRequest request = new(
            VenueId: venue2Id,
            ManifestTemplateId: templateForVenue1,
            Name: $"Mismatched Event {Guid.NewGuid()}",
            EventDate: DateTimeOffset.UtcNow.AddMonths(1),
            Description: null);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WhenEventDateIsInThePast_ReturnsBadRequest()
    {
        Guid venueId = await CreateVenueAsync();
        Guid templateId = await CreateManifestTemplateAsync(venueId);

        CreateEventRequest request = new(
            VenueId: venueId,
            ManifestTemplateId: templateId,
            Name: $"Past Event {Guid.NewGuid()}",
            EventDate: DateTimeOffset.UtcNow.AddYears(-1),
            Description: null);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WhenNameIsEmpty_ReturnsBadRequest()
    {
        Guid venueId = await CreateVenueAsync();
        Guid templateId = await CreateManifestTemplateAsync(venueId);

        CreateEventRequest request = new(
            VenueId: venueId,
            ManifestTemplateId: templateId,
            Name: "",
            EventDate: DateTimeOffset.UtcNow.AddMonths(1),
            Description: null);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
}
