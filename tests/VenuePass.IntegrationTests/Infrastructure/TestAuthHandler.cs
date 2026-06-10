using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VenuePass.IntegrationTests.Infrastructure;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string SubHeader = "X-Test-Sub";
    public const string RoleHeader = "X-Test-Role";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SubHeader, out var subValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string sub = subValues!;

        var claims = new List<Claim>
        {
            new("sub", sub),
            new(ClaimTypes.NameIdentifier, sub)
        };

        if (Request.Headers.TryGetValue(RoleHeader, out var role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role!));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
