using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using VenuePass.Modules.Events.Infrastructure;
using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Infrastructure;

public sealed class EventsIntegrationTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU25-ubuntu-22.04")
        .Build();

    public EventsApiFactory Factory { get; private set; } = null!;

    public HttpClient Client => Factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        Factory = new EventsApiFactory(_sqlContainer.GetConnectionString());

        await MigrateWithRetryAsync();
    }

    private async Task MigrateWithRetryAsync()
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using IServiceScope scope = Factory.Services.CreateScope();
                EventsDbContext dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
                await dbContext.Database.MigrateAsync();
                return;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await _sqlContainer.DisposeAsync();
    }
}
