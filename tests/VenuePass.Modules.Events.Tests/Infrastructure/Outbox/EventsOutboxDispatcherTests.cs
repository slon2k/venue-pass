using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VenuePass.BuildingBlocks.Messaging;
using VenuePass.Modules.Events.Infrastructure;
using VenuePass.Modules.Events.Infrastructure.Outbox;
using Xunit;

namespace VenuePass.Modules.Events.Tests.Infrastructure.Outbox;

public sealed class EventsOutboxDispatcherTests
{
    [Fact]
    public async Task DispatchBatchAsync_HappyPath_InvokesHandlerAndMarksProcessed()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var recorder = new HandlingRecorder();
        var services = CreateServices(
            dbName: Guid.NewGuid().ToString(),
            configureServices: s =>
            {
                s.AddSingleton(recorder);
                s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, RecordingHandler>();
            });

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "happy");
        var message = OutboxMessage.Create(now, typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));

        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        // Act
        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        // Assert
        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved!.ProcessedOn);
        Assert.Equal(0, saved.AttemptCount);
        Assert.Null(saved.Error);

        Assert.Equal(1, recorder.CallCount);
        var handled = Assert.Single(recorder.HandledEvents);
        Assert.Equal(payload.MessageId, handled.MessageId);
        Assert.Equal(payload.OccurredOn, handled.OccurredOn);
        Assert.Equal(payload.Value, handled.Value);
    }

    [Fact]
    public async Task DispatchBatchAsync_NoHandler_MarksProcessedGracefully()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 4, 12, 5, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var services = CreateServices(Guid.NewGuid().ToString());

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "no-handler");
        var message = OutboxMessage.Create(now, typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));
        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        // Act
        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        // Assert
        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved!.ProcessedOn);
        Assert.Equal(0, saved.AttemptCount);
        Assert.Null(saved.Error);
    }

    [Fact]
    public async Task DispatchBatchAsync_HandlerThrows_RecordsFailureAndSchedulesRetry()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 4, 12, 10, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var services = CreateServices(
            dbName: Guid.NewGuid().ToString(),
            configureServices: s => s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, ThrowingHandler>());

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "throws");
        var message = OutboxMessage.Create(now, typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));
        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        // Act
        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        // Assert
        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.Null(saved!.ProcessedOn);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(now, saved.LastAttemptedOn);
        Assert.Equal(now.AddSeconds(30), saved.NextAttemptOn);
        Assert.Equal(ThrowingHandler.ErrorMessage, saved.Error);
    }

    [Fact]
    public async Task DispatchBatchAsync_MessageNotYetEligible_SkipsMessage()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 4, 12, 20, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var recorder = new HandlingRecorder();
        var services = CreateServices(
            dbName: Guid.NewGuid().ToString(),
            configureServices: s =>
            {
                s.AddSingleton(recorder);
                s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, RecordingHandler>();
            });

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "not-eligible");
        var message = OutboxMessage.Create(now, typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));
        message.RecordFailure(now, now.AddMinutes(10), "previous failure");
        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        // Act
        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        // Assert
        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.Null(saved!.ProcessedOn);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(now.AddMinutes(10), saved.NextAttemptOn);
        Assert.Equal("previous failure", saved.Error);
        Assert.Equal(0, recorder.CallCount);
    }

    [Fact]
    public async Task DispatchBatchAsync_MaxAttemptsReached_MarksAbandonedWithError()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 4, 12, 30, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var services = CreateServices(
            dbName: Guid.NewGuid().ToString(),
            configureServices: s => s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, ThrowingHandler>());

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "max-attempts");
        var message = OutboxMessage.Create(now.AddMinutes(-10), typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));
        message.RecordFailure(now.AddMinutes(-9), now.AddMinutes(-8), "error-1");
        message.RecordFailure(now.AddMinutes(-7), now.AddMinutes(-6), "error-2");
        message.RecordFailure(now.AddMinutes(-5), now.AddMinutes(-4), "error-3");
        message.RecordFailure(now.AddMinutes(-3), now.AddMinutes(-2), "error-4");
        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        // Act
        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        // Assert
        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved!.ProcessedOn);
        Assert.Equal(5, saved.AttemptCount);
        Assert.Equal(now, saved.LastAttemptedOn);
        Assert.NotNull(saved.Error);
        Assert.StartsWith("ABANDONED:", saved.Error);
        Assert.Contains(ThrowingHandler.ErrorMessage, saved.Error);
    }

    [Fact]
    public async Task DispatchBatchAsync_UnresolvableType_MarksProcessedAsPoisonPill()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 4, 12, 35, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var services = CreateServices(Guid.NewGuid().ToString());

        var message = OutboxMessage.Create(
            occurredOn: now,
            type: "Missing.Type.Name, Missing.Assembly",
            payload: "{\"x\":1}");

        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        // Act
        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        // Assert
        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved!.ProcessedOn);
        Assert.Equal(0, saved.AttemptCount);
        Assert.Null(saved.Error);
    }

    [Fact]
    public async Task DispatchBatchAsync_InvalidJsonPayload_MarksProcessedAsPoisonPill()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 4, 12, 40, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var services = CreateServices(Guid.NewGuid().ToString());

        var message = OutboxMessage.Create(
            occurredOn: now,
            type: typeof(TestIntegrationEvent).AssemblyQualifiedName!,
            payload: "not-json");

        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        // Act
        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        // Assert
        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved!.ProcessedOn);
        Assert.Equal(0, saved.AttemptCount);
        Assert.Null(saved.Error);
    }

    [Fact]
    public async Task DispatchBatchAsync_AlreadyProcessedMessage_IsSkipped()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 4, 12, 45, 0, TimeSpan.Zero);
        var processedAt = now.AddMinutes(-1);
        var timeProvider = new FakeTimeProvider(now);
        var recorder = new HandlingRecorder();
        var services = CreateServices(
            dbName: Guid.NewGuid().ToString(),
            configureServices: s =>
            {
                s.AddSingleton(recorder);
                s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, RecordingHandler>();
            });

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "already-processed");
        var message = OutboxMessage.Create(
            occurredOn: now.AddMinutes(-10),
            type: typeof(TestIntegrationEvent).AssemblyQualifiedName!,
            payload: JsonSerializer.Serialize(payload));
        message.MarkProcessed(processedAt);

        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        // Act
        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        // Assert
        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.Equal(processedAt, saved!.ProcessedOn);
        Assert.Equal(0, recorder.CallCount);
    }

    private static ServiceProvider CreateServices(string dbName, Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();

        services.AddDbContext<EventsDbContext>(options => options.UseInMemoryDatabase(dbName));

        configureServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private static EventsOutboxDispatcher CreateDispatcher(IServiceProvider services, TimeProvider timeProvider)
        => new(services, timeProvider, NullLogger<EventsOutboxDispatcher>.Instance);

    private static async Task SeedMessageAsync(IServiceProvider services, OutboxMessage message)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();
    }

    private static async Task<OutboxMessage?> GetMessageAsync(IServiceProvider services, Guid id)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        return await db.OutboxMessages.SingleOrDefaultAsync(x => x.Id == id);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed record TestIntegrationEvent(Guid MessageId, DateTimeOffset OccurredOn, string Value) : IIntegrationEvent;

    private sealed class HandlingRecorder
    {
        public int CallCount => HandledEvents.Count;

        public List<TestIntegrationEvent> HandledEvents { get; } = [];
    }

    private sealed class RecordingHandler(HandlingRecorder recorder) : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task Handle(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            recorder.HandledEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public const string ErrorMessage = "simulated handler failure";

        public Task Handle(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => throw new InvalidOperationException(ErrorMessage);
    }
}