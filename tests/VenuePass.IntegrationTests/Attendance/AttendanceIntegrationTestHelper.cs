using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Infrastructure;

namespace VenuePass.IntegrationTests.Attendance;

internal static class AttendanceIntegrationTestHelper
{
    public static string BuildIsolatedConnectionString(string baseConnectionString, string databasePrefix)
    {
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = $"{databasePrefix}_{Guid.NewGuid():N}"
        };

        return builder.ConnectionString;
    }

    public static ServiceProvider CreateServicesWithEnsureCreated(string isolatedConnectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AttendanceDbContext>(options => options.UseSqlServer(isolatedConnectionString));

        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }

    public static async Task<ServiceProvider> CreateServicesWithMigrateAsync(string isolatedConnectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AttendanceDbContext>(options => options.UseSqlServer(isolatedConnectionString));

        ServiceProvider provider = services.BuildServiceProvider();

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        await db.Database.MigrateAsync();

        return provider;
    }

    public static async Task<PublishedEventReferenceId> SeedPublishedEventReferenceAsync(
        IServiceProvider services,
        PublishedEventReferenceId? id = null)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        PublishedEventReferenceId referenceId = id ?? new PublishedEventReferenceId(Guid.CreateVersion7());

        PublishedEventReference reference = PublishedEventReference.Create(
            id: referenceId,
            eventId: Guid.CreateVersion7(),
            manifestId: Guid.CreateVersion7(),
            syncedAt: DateTimeOffset.UtcNow);

        db.PublishedEventReferences.Add(reference);
        await db.SaveChangesAsync();

        return referenceId;
    }
}