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

        using IServiceScope scope = Factory.Services.CreateScope();
        EventsDbContext dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        await dbContext.Database.MigrateAsync();
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
