using System.Text.Json;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using VenuePass.BuildingBlocks.Messaging;
using VenuePass.IntegrationTests.Infrastructure;
using VenuePass.Modules.Events.Infrastructure;
using VenuePass.Modules.Events.Infrastructure.Outbox;

using Xunit;

namespace VenuePass.IntegrationTests.Events.Infrastructure;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class EventsOutboxDispatcherTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public EventsOutboxDispatcherTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DispatchBatchAsync_HappyPath_InvokesHandlerAndMarksProcessed()
    {
        var now = new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var recorder = new HandlingRecorder();
        var services = CreateServices(configureServices: s =>
        {
            s.AddSingleton(recorder);
            s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, RecordingHandler>();
        });

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "happy");
        var message = OutboxMessage.Create(now, typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));

        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

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
    public async Task DispatchBatchAsync_MultipleHandlers_InvokesEachHandlerAndMarksProcessed()
    {
        var now = new DateTimeOffset(2026, 6, 4, 12, 2, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var recorder = new HandlingRecorder();
        var services = CreateServices(configureServices: s =>
        {
            s.AddSingleton(recorder);
            s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, FirstRecordingHandler>();
            s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, SecondRecordingHandler>();
        });

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "multi-handler");
        var message = OutboxMessage.Create(now, typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));

        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved!.ProcessedOn);
        Assert.Equal(0, saved.AttemptCount);
        Assert.Null(saved.Error);

        Assert.Equal(2, recorder.Invocations.Count);
        Assert.Contains("first:multi-handler", recorder.Invocations);
        Assert.Contains("second:multi-handler", recorder.Invocations);
    }

    [Fact]
    public async Task DispatchBatchAsync_NoHandler_MarksProcessedGracefully()
    {
        var now = new DateTimeOffset(2026, 6, 4, 12, 5, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var services = CreateServices();

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "no-handler");
        var message = OutboxMessage.Create(now, typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));
        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved!.ProcessedOn);
        Assert.Equal(0, saved.AttemptCount);
        Assert.Null(saved.Error);
    }

    [Fact]
    public async Task DispatchBatchAsync_HandlerThrows_RecordsFailureAndSchedulesRetry()
    {
        var now = new DateTimeOffset(2026, 6, 4, 12, 10, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var services = CreateServices(configureServices: s =>
            s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, ThrowingHandler>());

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "throws");
        var message = OutboxMessage.Create(now, typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));
        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

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
        var now = new DateTimeOffset(2026, 6, 4, 12, 20, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var recorder = new HandlingRecorder();
        var services = CreateServices(configureServices: s =>
        {
            s.AddSingleton(recorder);
            s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, RecordingHandler>();
        });

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "not-eligible");
        var message = OutboxMessage.Create(now, typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));
        message.RecordFailure(now, now.AddMinutes(10), "previous failure");
        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

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
        var now = new DateTimeOffset(2026, 6, 4, 12, 30, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var services = CreateServices(configureServices: s =>
            s.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, ThrowingHandler>());

        var payload = new TestIntegrationEvent(Guid.CreateVersion7(), now, "max-attempts");
        var message = OutboxMessage.Create(now.AddMinutes(-10), typeof(TestIntegrationEvent).AssemblyQualifiedName!, JsonSerializer.Serialize(payload));
        message.RecordFailure(now.AddMinutes(-9), now.AddMinutes(-8), "error-1");
        message.RecordFailure(now.AddMinutes(-7), now.AddMinutes(-6), "error-2");
        message.RecordFailure(now.AddMinutes(-5), now.AddMinutes(-4), "error-3");
        message.RecordFailure(now.AddMinutes(-3), now.AddMinutes(-2), "error-4");
        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

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
        var now = new DateTimeOffset(2026, 6, 4, 12, 35, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var services = CreateServices();

        var message = OutboxMessage.Create(
            occurredOn: now,
            type: "Missing.Type.Name, Missing.Assembly",
            payload: "{\"x\":1}");

        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved!.ProcessedOn);
        Assert.Equal(0, saved.AttemptCount);
        Assert.Null(saved.Error);
    }

    [Fact]
    public async Task DispatchBatchAsync_InvalidJsonPayload_MarksProcessedAsPoisonPill()
    {
        var now = new DateTimeOffset(2026, 6, 4, 12, 40, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var services = CreateServices();

        var message = OutboxMessage.Create(
            occurredOn: now,
            type: typeof(TestIntegrationEvent).AssemblyQualifiedName!,
            payload: "not-json");

        await SeedMessageAsync(services, message);

        var dispatcher = CreateDispatcher(services, timeProvider);

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved!.ProcessedOn);
        Assert.Equal(0, saved.AttemptCount);
        Assert.Null(saved.Error);
    }

    [Fact]
    public async Task DispatchBatchAsync_AlreadyProcessedMessage_IsSkipped()
    {
        var now = new DateTimeOffset(2026, 6, 4, 12, 45, 0, TimeSpan.Zero);
        var processedAt = now.AddMinutes(-1);
        var timeProvider = new FakeTimeProvider(now);
        var recorder = new HandlingRecorder();
        var services = CreateServices(configureServices: s =>
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

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        var saved = await GetMessageAsync(services, message.Id);
        Assert.NotNull(saved);
        Assert.Equal(processedAt, saved!.ProcessedOn);
        Assert.Equal(0, recorder.CallCount);
    }

    // ── Infrastructure ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a ServiceProvider backed by an isolated SQL Server database.
    /// Each call gets a unique database (via a unique Initial Catalog) to avoid
    /// cross-test contamination of the outbox table.
    /// </summary>
    private ServiceProvider CreateServices(Action<IServiceCollection>? configureServices = null)
    {
        var isolatedConnectionString = BuildIsolatedConnectionString();

        var services = new ServiceCollection();
        services.AddDbContext<EventsDbContext>(options => options.UseSqlServer(isolatedConnectionString));
        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider();

        // Create schema for this isolated database.
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }

    private string BuildIsolatedConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            InitialCatalog = $"outbox_test_{Guid.NewGuid():N}"
        };
        return builder.ConnectionString;
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

    // ── Test doubles ──────────────────────────────────────────────────────────

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed record TestIntegrationEvent(Guid MessageId, DateTimeOffset OccurredOn, string Value) : IIntegrationEvent;

    private sealed class HandlingRecorder
    {
        public int CallCount => HandledEvents.Count;
        public List<TestIntegrationEvent> HandledEvents { get; } = [];
        public List<string> Invocations { get; } = [];
    }

    private sealed class RecordingHandler(HandlingRecorder recorder) : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task Handle(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            recorder.HandledEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FirstRecordingHandler(HandlingRecorder recorder) : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task Handle(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            recorder.Invocations.Add($"first:{integrationEvent.Value}");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondRecordingHandler(HandlingRecorder recorder) : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task Handle(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            recorder.Invocations.Add($"second:{integrationEvent.Value}");
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
