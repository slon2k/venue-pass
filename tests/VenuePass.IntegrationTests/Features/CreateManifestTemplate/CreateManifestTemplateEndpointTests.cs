using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Infrastructure;
using VenuePass.Modules.Events.IntegrationTests.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Features.CreateManifestTemplate;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class CreateManifestTemplateEndpointTests
{
    private readonly EventsIntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public CreateManifestTemplateEndpointTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAdminClient();
    }

    [Fact]
    public async Task CreateManifestTemplate_WhenRequestIsValid_ReturnsCreatedAndPersistsStructure()
    {
        Guid venueId = await CreateVenueAsync();

        var templateName = $"Main Template {Guid.NewGuid()}";

        CreateManifestTemplateRequest request = new(
            Name: $"  {templateName}  ",
            Description: "Template used for a seated concert.",
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
        Assert.NotNull(response.Headers.Location);
        Assert.NotNull(body);

        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        ManifestTemplate? persistedTemplate = await db.ManifestTemplates
            .AsNoTracking()
            .Include(x => x.Sections)
            .ThenInclude(x => x.Rows)
            .ThenInclude(x => x.Seats)
            .Include(x => x.GeneralAdmissionAreas)
            .FirstOrDefaultAsync(x => x.Id == new ManifestTemplateId(body.ManifestTemplateId));

        Assert.NotNull(persistedTemplate);
        Assert.Equal(templateName, persistedTemplate.Name.Value);
        Assert.Equal(venueId, persistedTemplate.VenueId.Value);

        Assert.Single(persistedTemplate.Sections);
        Assert.Equal("Floor", persistedTemplate.Sections[0].Name.Value);

        Assert.Single(persistedTemplate.Sections[0].Rows);
        Assert.Equal("A", persistedTemplate.Sections[0].Rows[0].Label.Value);

        Assert.Equal(2, persistedTemplate.Sections[0].Rows[0].Seats.Count);
        Assert.Contains(persistedTemplate.Sections[0].Rows[0].Seats, seat => seat.Label.Value == "1");
        Assert.Contains(persistedTemplate.Sections[0].Rows[0].Seats, seat => seat.Label.Value == "2");

        Assert.Single(persistedTemplate.GeneralAdmissionAreas);
        Assert.Equal("GA East", persistedTemplate.GeneralAdmissionAreas[0].Name.Value);
        Assert.Equal(300, persistedTemplate.GeneralAdmissionAreas[0].Capacity.Value);
    }

    [Fact]
    public async Task CreateManifestTemplate_WhenVenueDoesNotExist_ReturnsNotFound()
    {
        CreateManifestTemplateRequest request = new(
            Name: $"Missing Venue Template {Guid.NewGuid()}",
            Description: null,
            VenueId: Guid.NewGuid(),
            Sections:
            [
                new CreateManifestTemplateSectionRequest(
                    Name: "Main",
                    Rows:
                    [
                        new CreateManifestTemplateRowRequest(
                            Label: "A",
                            Seats:
                            [
                                new CreateManifestTemplateSeatRequest("1")
                            ])
                    ])
            ],
            GeneralAdmissionAreas: []);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events/manifest-templates", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateManifestTemplate_WhenNoLayoutElements_ReturnsBadRequest()
    {
        Guid venueId = await CreateVenueAsync();

        CreateManifestTemplateRequest request = new(
            Name: $"Invalid Template {Guid.NewGuid()}",
            Description: null,
            VenueId: venueId,
            Sections: [],
            GeneralAdmissionAreas: []);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events/manifest-templates", request);

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

        HttpResponseMessage response = await _client.PostAsJsonAsync("/events/venues", request);
        CreateVenueResponse? body = await response.Content.ReadFromJsonAsync<CreateVenueResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);

        return body.VenueId;
    }

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
