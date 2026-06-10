using System.Net;
using System.Net.Http.Json;

using VenuePass.Modules.Events.IntegrationTests.Infrastructure;

using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Features.Auth;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class AuthEnforcementTests
{
    private readonly HttpClient _unauthenticated;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _managerClient;

    public AuthEnforcementTests(EventsIntegrationTestFixture fixture)
    {
        _unauthenticated = fixture.Client;
        _adminClient = fixture.CreateAdminClient();
        _managerClient = fixture.CreateEventManagerClient();
    }

    [Fact]
    public async Task PostVenue_WhenUnauthenticated_Returns401()
    {
        HttpResponseMessage response = await _unauthenticated.PostAsJsonAsync("/events/venues", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostVenue_WhenCallerIsEventManager_Returns403()
    {
        HttpResponseMessage response = await _managerClient.PostAsJsonAsync("/events/venues", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostManifestTemplate_WhenUnauthenticated_Returns401()
    {
        HttpResponseMessage response = await _unauthenticated.PostAsJsonAsync("/events/manifest-templates", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostManifestTemplate_WhenCallerIsEventManager_Returns403()
    {
        HttpResponseMessage response = await _managerClient.PostAsJsonAsync("/events/manifest-templates", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostEvent_WhenUnauthenticated_Returns401()
    {
        HttpResponseMessage response = await _unauthenticated.PostAsJsonAsync("/events", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostEvent_WhenCallerIsAdmin_Returns403()
    {
        HttpResponseMessage response = await _adminClient.PostAsJsonAsync("/events", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_WhenUnauthenticated_Returns401()
    {
        HttpResponseMessage response = await _unauthenticated.GetAsync($"/events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
