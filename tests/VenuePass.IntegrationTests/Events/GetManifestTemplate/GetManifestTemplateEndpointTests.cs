using System.Net;
using System.Net.Http.Json;

using VenuePass.IntegrationTests.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Events.GetManifestTemplate;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class GetManifestTemplateEndpointTests
{
    private readonly HttpClient _client;

    public GetManifestTemplateEndpointTests(EventsIntegrationTestFixture fixture)
    {
        _client = fixture.CreateAdminClient();
    }

    [Fact]
    public async Task GetManifestTemplate_WhenTemplateExists_ReturnsNestedLayout()
    {
        Guid venueId = await CreateVenueAsync();
        Guid templateId = await CreateManifestTemplateAsync(venueId);

        HttpResponseMessage response = await _client.GetAsync($"/events/manifest-templates/{templateId}");
        GetManifestTemplateResponse? body = await response.Content.ReadFromJsonAsync<GetManifestTemplateResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);

        Assert.Equal(templateId, body.ManifestTemplateId);
        Assert.Equal(venueId, body.VenueId);
        Assert.Equal("Main Template", body.Name);
        Assert.Equal("Concert layout", body.Description);

        Assert.Single(body.Sections);
        Assert.Equal("Floor", body.Sections[0].Name);

        Assert.Single(body.Sections[0].Rows);
        Assert.Equal("A", body.Sections[0].Rows[0].Label);

        Assert.Equal(2, body.Sections[0].Rows[0].Seats.Count);
        Assert.Contains(body.Sections[0].Rows[0].Seats, seat => seat.Label == "1");
        Assert.Contains(body.Sections[0].Rows[0].Seats, seat => seat.Label == "2");

        Assert.Single(body.GeneralAdmissionAreas);
        Assert.Equal("GA East", body.GeneralAdmissionAreas[0].Name);
        Assert.Equal(300, body.GeneralAdmissionAreas[0].Capacity);
    }

    [Fact]
    public async Task GetManifestTemplate_WhenTemplateDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();

        HttpResponseMessage response = await _client.GetAsync($"/events/manifest-templates/{id}");

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

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events/venues", request);
        CreateVenueResponse? body = await response.Content.ReadFromJsonAsync<CreateVenueResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);

        return body.VenueId;
    }

    private async Task<Guid> CreateManifestTemplateAsync(Guid venueId)
    {
        CreateManifestTemplateRequest request = new(
            Name: "Main Template",
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

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events/manifest-templates", request);
        CreateManifestTemplateResponse? body = await response.Content.ReadFromJsonAsync<CreateManifestTemplateResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);

        return body.ManifestTemplateId;
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

    private sealed record GetManifestTemplateResponse(
        Guid ManifestTemplateId,
        string Name,
        string? Description,
        Guid VenueId,
        IReadOnlyList<GetManifestTemplateSectionResponse> Sections,
        IReadOnlyList<GetManifestTemplateGeneralAdmissionAreaResponse> GeneralAdmissionAreas);

    private sealed record GetManifestTemplateSectionResponse(
        string Name,
        IReadOnlyList<GetManifestTemplateRowResponse> Rows);

    private sealed record GetManifestTemplateRowResponse(
        string Label,
        IReadOnlyList<GetManifestTemplateSeatResponse> Seats);

    private sealed record GetManifestTemplateSeatResponse(string Label);

    private sealed record GetManifestTemplateGeneralAdmissionAreaResponse(
        string Name,
        int Capacity);
}
