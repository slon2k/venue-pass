using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.TicketProjections;
using VenuePass.Modules.Attendance.Features.TicketCanceled;
using VenuePass.Modules.Attendance.Features.TicketIssued;
using VenuePass.Modules.Attendance.Infrastructure;
using VenuePass.Modules.Ticketing.Contracts;

using Xunit;

namespace VenuePass.IntegrationTests.Attendance.TicketProjections;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class TicketCanceledProjectionHandlerTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public TicketCanceledProjectionHandlerTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_CanceledBeforeIssued_CreatesProjection()
    {
        // Arrange: Simulate scenario where TicketCanceled event arrives before TicketIssued
        ServiceProvider services = CreateServices(
            AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
                _fixture.ConnectionString, "attendance_canceled_test"));

        PublishedEventReferenceId publishedEventReferenceId =
            await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        TicketCanceledIntegrationEvent canceledEvent = CreateCanceledIntegrationEvent(
            ticketCode: "0000000000000001",
            publishedEventReferenceId: publishedEventReferenceId.Value);

        // Act
        await InvokeHandlerAsync(services, canceledEvent);

        // Assert: Projection should exist and be marked Canceled
        await using AsyncServiceScope assertScope = services.CreateAsyncScope();
        AttendanceDbContext db = assertScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        TicketProjection? projection = await db.TicketProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == new TicketId(canceledEvent.TicketId));

        Assert.NotNull(projection);
        Assert.Equal(canceledEvent.TicketId, projection.Id.Value);
        Assert.Equal(canceledEvent.TicketCode, projection.TicketCode.Value);
        Assert.Equal(TicketProjectionStatus.Canceled, projection.Status);
        Assert.Equal(canceledEvent.OrderId, projection.OrderId.Value);
        Assert.Equal(canceledEvent.OrderItemId, projection.OrderItemId.Value);
        Assert.Equal(canceledEvent.PublishedEventReferenceId, projection.PublishedEventReferenceId.Value);
        Assert.Equal(canceledEvent.InventoryId, projection.InventoryId.Value);
        Assert.Equal(canceledEvent.InventorySeatId, projection.InventorySeatId?.Value);
        Assert.Null(projection.GeneralAdmissionPoolId);
    }

    [Fact]
    public async Task Handle_ExistingProjection_MarksAsCanceled()
    {
        // Arrange: First create an Issued projection, then cancel it
        ServiceProvider services = CreateServices(
            AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
                _fixture.ConnectionString, "attendance_mark_canceled_test"));

        PublishedEventReferenceId publishedEventReferenceId =
            await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        TicketIssuedIntegrationEvent issuedEvent = CreateIssuedIntegrationEvent(
            ticketCode: "0000000000000002",
            publishedEventReferenceId: publishedEventReferenceId.Value);

        await InvokeHandlerAsync(services, issuedEvent);

        // Act: Now cancel the same ticket
        TicketCanceledIntegrationEvent canceledEvent = CreateCanceledIntegrationEvent(
            ticketCode: issuedEvent.TicketCode,
            publishedEventReferenceId: publishedEventReferenceId.Value,
            ticketId: issuedEvent.TicketId,
            orderId: issuedEvent.OrderId,
            orderItemId: issuedEvent.OrderItemId,
            inventoryId: issuedEvent.InventoryId,
            inventorySeatId: issuedEvent.InventorySeatId,
            generalAdmissionPoolId: issuedEvent.GeneralAdmissionPoolId);

        await InvokeHandlerAsync(services, canceledEvent);

        // Assert: Projection should be marked Canceled and only 1 row exists
        await using AsyncServiceScope assertScope = services.CreateAsyncScope();
        AttendanceDbContext db = assertScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        int count = await db.TicketProjections
            .AsNoTracking()
            .CountAsync(x => x.Id == new TicketId(canceledEvent.TicketId));

        Assert.Equal(1, count);

        TicketProjection? projection = await db.TicketProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == new TicketId(canceledEvent.TicketId));

        Assert.NotNull(projection);
        Assert.Equal(TicketProjectionStatus.Canceled, projection.Status);
    }

    [Fact]
    public async Task Handle_ReplayedDelivery_IsIdempotent()
    {
        // Arrange: Create and replay the same TicketCanceled event
        ServiceProvider services = CreateServices(
            AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
                _fixture.ConnectionString, "attendance_canceled_replay_test"));

        PublishedEventReferenceId publishedEventReferenceId =
            await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        TicketCanceledIntegrationEvent canceledEvent = CreateCanceledIntegrationEvent(
            ticketCode: "0000000000000003",
            publishedEventReferenceId: publishedEventReferenceId.Value);

        // Act: First delivery and exact replay
        await InvokeHandlerAsync(services, canceledEvent);
        await InvokeHandlerAsync(services, canceledEvent); // exact replay of same event

        // Assert: Only 1 row should exist (idempotent)
        await using AsyncServiceScope assertScope = services.CreateAsyncScope();
        AttendanceDbContext db = assertScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        int count = await db.TicketProjections
            .AsNoTracking()
            .CountAsync(x => x.Id == new TicketId(canceledEvent.TicketId));

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Handle_LaterTicketIssuedDoesNotRevertCanceledState()
    {
        // Arrange: Cancel first, then try to issue (race condition where canceled comes before issued in processing)
        ServiceProvider services = CreateServices(
            AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
                _fixture.ConnectionString, "attendance_convergence_test"));

        PublishedEventReferenceId publishedEventReferenceId =
            await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        var ticketCode = "0000000000000004";
        var ticketId = Guid.CreateVersion7();

        TicketCanceledIntegrationEvent canceledEvent = CreateCanceledIntegrationEvent(
            ticketCode: ticketCode,
            ticketId: ticketId,
            publishedEventReferenceId: publishedEventReferenceId.Value);

        // Act: Cancel first
        await InvokeHandlerAsync(services, canceledEvent);

        // Act: Then issue (with later OccurredOn timestamp)
        TicketIssuedIntegrationEvent issuedEvent = CreateIssuedIntegrationEvent(
            ticketCode: ticketCode,
            ticketId: ticketId,
            orderId: canceledEvent.OrderId,
            orderItemId: canceledEvent.OrderItemId,
            inventoryId: canceledEvent.InventoryId,
            inventorySeatId: canceledEvent.InventorySeatId,
            generalAdmissionPoolId: canceledEvent.GeneralAdmissionPoolId,
            publishedEventReferenceId: publishedEventReferenceId.Value,
            occurredOn: canceledEvent.OccurredOn.AddSeconds(5)); // Later than canceled

        await InvokeHandlerAsync(services, issuedEvent);

        // Assert: Projection should remain Canceled, not revert to Issued
        await using AsyncServiceScope assertScope = services.CreateAsyncScope();
        AttendanceDbContext db = assertScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        TicketProjection? projection = await db.TicketProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == new TicketId(ticketId));

        Assert.NotNull(projection);
        Assert.Equal(TicketProjectionStatus.Canceled, projection.Status);

        int count = await db.TicketProjections
            .AsNoTracking()
            .CountAsync(x => x.Id == new TicketId(ticketId));

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Handle_DuplicateDeliveryWithDifferentMessageId_DoesNotCreateDuplicateProjection()
    {
        // Arrange: At-least-once delivery scenario with same event data but different MessageId
        ServiceProvider services = CreateServices(
            AttendanceIntegrationTestHelper.BuildIsolatedConnectionString(
                _fixture.ConnectionString, "attendance_duplicate_canceled_test"));

        PublishedEventReferenceId publishedEventReferenceId =
            await AttendanceIntegrationTestHelper.SeedPublishedEventReferenceAsync(services);

        TicketCanceledIntegrationEvent firstDelivery = CreateCanceledIntegrationEvent(
            ticketCode: "0000000000000005",
            publishedEventReferenceId: publishedEventReferenceId.Value);

        TicketCanceledIntegrationEvent secondDelivery = firstDelivery with { MessageId = Guid.NewGuid() };

        // Act: First and second delivery (same event, different MessageId)
        await InvokeHandlerAsync(services, firstDelivery);
        await InvokeHandlerAsync(services, secondDelivery);

        // Assert: Only 1 row should exist (at-least-once semantics handled correctly)
        await using AsyncServiceScope assertScope = services.CreateAsyncScope();
        AttendanceDbContext db = assertScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

        int count = await db.TicketProjections
            .AsNoTracking()
            .CountAsync(x => x.Id == new TicketId(firstDelivery.TicketId));

        Assert.Equal(1, count);

        TicketProjection? projection = await db.TicketProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == new TicketId(firstDelivery.TicketId));

        Assert.NotNull(projection);
        Assert.Equal(TicketProjectionStatus.Canceled, projection.Status);
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
        TicketCanceledIntegrationEvent integrationEvent)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AttendanceDbContext db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var handler = new TicketCanceledHandler(db, NullLogger<TicketCanceledHandler>.Instance);
        await handler.Handle(integrationEvent, CancellationToken.None);
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

    private static TicketCanceledIntegrationEvent CreateCanceledIntegrationEvent(
        string ticketCode,
        Guid publishedEventReferenceId,
        Guid? ticketId = null,
        Guid? orderId = null,
        Guid? orderItemId = null,
        Guid? inventoryId = null,
        Guid? inventorySeatId = null,
        Guid? generalAdmissionPoolId = null) =>
        new(
            MessageId: Guid.NewGuid(),
            TicketId: ticketId ?? Guid.CreateVersion7(),
            TicketCode: ticketCode,
            OrderId: orderId ?? Guid.CreateVersion7(),
            OrderItemId: orderItemId ?? Guid.CreateVersion7(),
            PublishedEventReferenceId: publishedEventReferenceId,
            InventoryId: inventoryId ?? Guid.CreateVersion7(),
            InventorySeatId: inventorySeatId ?? Guid.CreateVersion7(),
            GeneralAdmissionPoolId: generalAdmissionPoolId,
            OccurredOn: DateTimeOffset.UtcNow);

    private static TicketIssuedIntegrationEvent CreateIssuedIntegrationEvent(
        string ticketCode,
        Guid publishedEventReferenceId,
        Guid? ticketId = null,
        Guid? orderId = null,
        Guid? orderItemId = null,
        Guid? inventoryId = null,
        Guid? inventorySeatId = null,
        Guid? generalAdmissionPoolId = null,
        DateTimeOffset? occurredOn = null) =>
        new(
            MessageId: Guid.NewGuid(),
            TicketId: ticketId ?? Guid.CreateVersion7(),
            TicketCode: ticketCode,
            OrderId: orderId ?? Guid.CreateVersion7(),
            OrderItemId: orderItemId ?? Guid.CreateVersion7(),
            PublishedEventReferenceId: publishedEventReferenceId,
            InventoryId: inventoryId ?? Guid.CreateVersion7(),
            InventorySeatId: inventorySeatId ?? Guid.CreateVersion7(),
            GeneralAdmissionPoolId: generalAdmissionPoolId,
            OccurredOn: occurredOn ?? DateTimeOffset.UtcNow);
}
