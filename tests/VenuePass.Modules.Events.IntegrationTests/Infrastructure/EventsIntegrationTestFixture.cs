using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using VenuePass.Modules.Events.Infrastructure;
using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Infrastructure;

public sealed class EventsIntegrationTestFixture : IAsyncLifetime
{
    private const int MaxMigrationAttempts = 10;

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("yourStrong(!)Password")
        .Build();

    public EventsApiFactory Factory { get; private set; } = null!;

    public HttpClient Client => Factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        Factory = new EventsApiFactory(_sqlContainer.GetConnectionString());

        await MigrateDatabaseWithRetryAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await _sqlContainer.DisposeAsync();
    }

    private async Task MigrateDatabaseWithRetryAsync()
    {
        for (var attempt = 1; attempt <= MaxMigrationAttempts; attempt++)
        {
            try
            {
                using IServiceScope scope = Factory.Services.CreateScope();
                EventsDbContext dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
                await dbContext.Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (IsTransientDatabaseStartupError(ex) && attempt < MaxMigrationAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(attempt * 2, 10));
                await Task.Delay(delay);
            }
        }

        using IServiceScope finalScope = Factory.Services.CreateScope();
        EventsDbContext finalDbContext = finalScope.ServiceProvider.GetRequiredService<EventsDbContext>();
        await finalDbContext.Database.MigrateAsync();
    }

    private static bool IsTransientDatabaseStartupError(Exception exception)
    {
        if (exception is SqlException)
        {
            return true;
        }

        return exception.InnerException is not null && IsTransientDatabaseStartupError(exception.InnerException);
    }
}
