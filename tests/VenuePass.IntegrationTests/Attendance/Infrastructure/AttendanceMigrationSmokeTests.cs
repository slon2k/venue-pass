using System.Globalization;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using VenuePass.IntegrationTests.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Attendance.Infrastructure;


[Collection(EventsTestCollectionFixture.Name)]
public sealed class AttendanceMigrationSmokeTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public AttendanceMigrationSmokeTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrate_AttendanceSchema_AppliesCleanly()
    {
        string isolatedConnectionString = AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
            _fixture.ConnectionString,
            "attendance_migration_smoke");

        await using ServiceProvider provider = await AttendanceIntegrationTestHelper.CreateServicesWithMigrateAsync(
            isolatedConnectionString);

        await using var connection = new SqlConnection(isolatedConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sys.tables AS t
            INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
            WHERE s.name = 'attendance'
              AND t.name IN (
                  'published_event_references',
                  'scan_attempts',
                  'attendance_records',
                  'ticket_projections',
                  'outbox_messages'
              )
            """;

        object? scalar = await command.ExecuteScalarAsync();
        int tableCount = Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
        Assert.Equal(5, tableCount);
    }
}
