using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using VenuePass.Modules.Events.Infrastructure;
using VenuePass.Modules.Ticketing.Infrastructure;
using Xunit;

namespace VenuePass.IntegrationTests.Infrastructure;

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

    public string ConnectionString { get; private set; } = null!;

    public HttpClient Client => Factory.CreateClient();

    public EventsApiFactory CreateFactory(
        bool enableOutboxDispatcher = false,
        Action<IServiceCollection>? configureTestServices = null)
        => new(ConnectionString, enableOutboxDispatcher, configureTestServices);

    public HttpClient CreateAdminClient(string? userId = null)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, userId ?? Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventAdmin");
        return client;
    }

    public HttpClient CreateEventManagerClient(string? userId = null)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, userId ?? Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventManager");
        return client;
    }

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

        ConnectionString = connectionString;

        Factory = new EventsApiFactory(ConnectionString);

        using IServiceScope scope = Factory.Services.CreateScope();
        EventsDbContext dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        await dbContext.Database.MigrateAsync();

        TicketingDbContext ticketingDbContext = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
        await ticketingDbContext.Database.MigrateAsync();
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
