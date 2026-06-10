using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using VenuePass.Modules.Events.Infrastructure.Outbox;

namespace VenuePass.Modules.Events.IntegrationTests.Infrastructure;

public sealed class EventsApiFactory(
    string connectionString,
    bool enableOutboxDispatcher = false,
    Action<IServiceCollection>? configureTestServices = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Database", connectionString);

        builder.ConfigureServices(services =>
        {
            if (!enableOutboxDispatcher)
            {
                services.RemoveAll<IHostedService>();
                services.AddHostedService<TestNoopHostedService>();
            }

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            configureTestServices?.Invoke(services);
        });
    }

    private sealed class TestNoopHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
