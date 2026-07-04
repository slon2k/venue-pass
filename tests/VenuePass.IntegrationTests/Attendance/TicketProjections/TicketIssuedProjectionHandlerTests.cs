using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.TicketProjections;
using VenuePass.Modules.Attendance.Features.TicketIssued;
using VenuePass.Modules.Attendance.Infrastructure;
using VenuePass.Modules.Ticketing.Contracts;

using Xunit;

namespace VenuePass.IntegrationTests.Attendance.TicketProjections;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class TicketIssuedProjectionHandlerTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public TicketIssuedProjectionHandlerTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_FirstDelivery_CreatesProjection()
    {
        ServiceProvider services = CreateServices(
            AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
                _fixture.ConnectionString, "attendance_handler_test"));

        PublishedEventReferenceId publishedEventReferenceId =
            await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        TicketIssuedIntegrationEvent integrationEvent = CreateIntegrationEvent(
            ticketCode: "0000000000000001",
            publishedEventReferenceId: publishedEventReferenceId.Value);

        await InvokeHandlerAsync(services, integrationEvent);

        await using AsyncServiceScope assertScope = services.CreateAsyncScope();
        AttendanceDbContext db = assertScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        TicketProjection? projection = await db.TicketProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == new TicketId(integrationEvent.TicketId));

        Assert.NotNull(projection);
        Assert.Equal(integrationEvent.TicketId, projection.Id.Value);
        Assert.Equal(integrationEvent.TicketCode, projection.TicketCode.Value);
        Assert.Equal(TicketProjectionStatus.Issued, projection.Status);
        Assert.Equal(integrationEvent.OrderId, projection.OrderId.Value);
        Assert.Equal(integrationEvent.OrderItemId, projection.OrderItemId.Value);
        Assert.Equal(integrationEvent.PublishedEventReferenceId, projection.PublishedEventReferenceId.Value);
        Assert.Equal(integrationEvent.InventoryId, projection.InventoryId.Value);
        Assert.Equal(integrationEvent.InventorySeatId, projection.InventorySeatId?.Value);
        Assert.Null(projection.GeneralAdmissionPoolId);
    }

    [Fact]
    public async Task Handle_ReplayedDelivery_IsIdempotent()
    {
        ServiceProvider services = CreateServices(
            AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
                _fixture.ConnectionString, "attendance_handler_test"));

        PublishedEventReferenceId publishedEventReferenceId =
            await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        TicketIssuedIntegrationEvent integrationEvent = CreateIntegrationEvent(
            ticketCode: "0000000000000002",
            publishedEventReferenceId: publishedEventReferenceId.Value);

        await InvokeHandlerAsync(services, integrationEvent);
        await InvokeHandlerAsync(services, integrationEvent); // exact replay of same event

        await using AsyncServiceScope assertScope = services.CreateAsyncScope();
        AttendanceDbContext db = assertScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        int count = await db.TicketProjections
            .AsNoTracking()
            .CountAsync(x => x.Id == new TicketId(integrationEvent.TicketId));

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Handle_DuplicateDeliveryWithDifferentMessageId_DoesNotCreateDuplicateProjection()
    {
        ServiceProvider services = CreateServices(
            AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
                _fixture.ConnectionString, "attendance_handler_test"));

        PublishedEventReferenceId publishedEventReferenceId =
            await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        TicketIssuedIntegrationEvent firstDelivery = CreateIntegrationEvent(
            ticketCode: "0000000000000003",
            publishedEventReferenceId: publishedEventReferenceId.Value);

        TicketIssuedIntegrationEvent secondDelivery = firstDelivery with { MessageId = Guid.NewGuid() };

        await InvokeHandlerAsync(services, firstDelivery);
        await InvokeHandlerAsync(services, secondDelivery);

        await using AsyncServiceScope assertScope = services.CreateAsyncScope();
        AttendanceDbContext db = assertScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        int count = await db.TicketProjections
            .AsNoTracking()
            .CountAsync(x => x.Id == new TicketId(firstDelivery.TicketId));

        Assert.Equal(1, count);
    }

    private static ServiceProvider CreateServices(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AttendanceDbContext>(options => options.UseSqlServer(connectionString));

        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }

    private static async Task InvokeHandlerAsync(
        IServiceProvider services,
        TicketIssuedIntegrationEvent integrationEvent)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var handler = new TicketIssuedHandler(db, NullLogger<TicketIssuedHandler>.Instance);
        await handler.Handle(integrationEvent, CancellationToken.None);
    }

    private static TicketIssuedIntegrationEvent CreateIntegrationEvent(
        string ticketCode,
        Guid publishedEventReferenceId) =>
        new(
            MessageId: Guid.NewGuid(),
            TicketId: Guid.CreateVersion7(),
            TicketCode: ticketCode,
            OrderId: Guid.CreateVersion7(),
            OrderItemId: Guid.CreateVersion7(),
            PublishedEventReferenceId: publishedEventReferenceId,
            InventoryId: Guid.CreateVersion7(),
            InventorySeatId: Guid.CreateVersion7(),
            GeneralAdmissionPoolId: null,
            OccurredOn: DateTimeOffset.UtcNow);
}
