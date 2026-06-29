using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.TicketProjections;
using VenuePass.Modules.Attendance.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Attendance.TicketProjections;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class TicketProjectionPersistenceTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public TicketProjectionPersistenceTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Save_DuplicateTicketId_IsRejectedByPersistence()
    {
        ServiceProvider services = CreateServices();
        await SeedPublishedEventReferenceAsync(services);

        var duplicateTicketId = new TicketId(Guid.CreateVersion7());

        var first = CreateProjection(
            ticketId: duplicateTicketId,
            ticketCode: new TicketCode("0000000000000001"));

        var second = CreateProjection(
            ticketId: duplicateTicketId,
            ticketCode: new TicketCode("0000000000000002"));

        await SaveProjectionAsync(services, first);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await SaveProjectionAsync(services, second));
    }

    [Fact]
    public async Task Save_DuplicateTicketCode_IsRejectedByPersistence()
    {
        ServiceProvider services = CreateServices();
        await SeedPublishedEventReferenceAsync(services);

        var duplicateCode = new TicketCode("0000000000000003");

        var first = CreateProjection(
            ticketId: new TicketId(Guid.CreateVersion7()),
            ticketCode: duplicateCode);

        var second = CreateProjection(
            ticketId: new TicketId(Guid.CreateVersion7()),
            ticketCode: duplicateCode);

        await SaveProjectionAsync(services, first);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await SaveProjectionAsync(services, second));
    }

    [Fact]
    public async Task Lookup_ByTicketIdAndTicketCode_ReturnsStoredProjection()
    {
        ServiceProvider services = CreateServices();
        await SeedPublishedEventReferenceAsync(services);

        var projection = CreateProjection(
            ticketId: new TicketId(Guid.CreateVersion7()),
            ticketCode: new TicketCode("0000000000000004"));

        await SaveProjectionAsync(services, projection);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        TicketProjection? byTicketId = await db.TicketProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == projection.Id);

        TicketProjection? byTicketCode = await db.TicketProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TicketCode == projection.TicketCode);

        Assert.NotNull(byTicketId);
        Assert.NotNull(byTicketCode);
        Assert.Equal(projection.Id, byTicketId!.Id);
        Assert.Equal(projection.Id, byTicketCode!.Id);
    }

    private ServiceProvider CreateServices()
    {
        string isolatedConnectionString = BuildIsolatedConnectionString();

        var services = new ServiceCollection();
        services.AddDbContext<AttendanceDbContext>(options => options.UseSqlServer(isolatedConnectionString));

        ServiceProvider provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }

    private string BuildIsolatedConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            InitialCatalog = $"attendance_projection_test_{Guid.NewGuid():N}"
        };

        return builder.ConnectionString;
    }

    private static async Task SeedPublishedEventReferenceAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        var reference = PublishedEventReference.Create(
            id: new PublishedEventReferenceId(KnownPublishedEventReferenceId),
            eventId: Guid.CreateVersion7(),
            manifestId: Guid.CreateVersion7(),
            syncedAt: DateTimeOffset.UtcNow);

        db.PublishedEventReferences.Add(reference);
        await db.SaveChangesAsync();
    }

    private static async Task SaveProjectionAsync(IServiceProvider services, TicketProjection projection)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        db.TicketProjections.Add(projection);
        await db.SaveChangesAsync();
    }

    private static TicketProjection CreateProjection(TicketId ticketId, TicketCode ticketCode)
        => TicketProjection.Create(
            id: ticketId,
            ticketCode: ticketCode,
            publishedEventReferenceId: new PublishedEventReferenceId(KnownPublishedEventReferenceId),
            orderId: new OrderId(Guid.CreateVersion7()),
            orderItemId: new OrderItemId(Guid.CreateVersion7()),
            inventoryId: new InventoryId(Guid.CreateVersion7()),
            inventorySeatId: new InventorySeatId(Guid.CreateVersion7()),
            generalAdmissionPoolId: null,
            issuedAt: DateTimeOffset.UtcNow);

    private static readonly Guid KnownPublishedEventReferenceId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
}
