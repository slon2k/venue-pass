using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using VenuePass.Modules.Events.Infrastructure;
using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Infrastructure;

public sealed class EventsIntegrationTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer? _sqlContainer;
    private readonly string? _externalConnectionString;

    public EventsIntegrationTestFixture()
    {
        _externalConnectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(_externalConnectionString))
        {
            _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU25-ubuntu-22.04")
                .Build();
        }
    }

    public EventsApiFactory Factory { get; private set; } = null!;

    public HttpClient Client => Factory.CreateClient();

    public async Task InitializeAsync()
    {
        string connectionString;

        if (_sqlContainer is not null)
        {
            await _sqlContainer.StartAsync();
            connectionString = _sqlContainer.GetConnectionString();
        }
        else
        {
            connectionString = _externalConnectionString!;
        }

        Factory = new EventsApiFactory(connectionString);

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

        if (_sqlContainer is not null)
        {
            await _sqlContainer.DisposeAsync();
        }
    }
}
