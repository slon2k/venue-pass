using System.Reflection;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Messaging;

namespace VenuePass.Modules.Attendance.Infrastructure.Outbox;

internal sealed class AttendanceOutboxDispatcher(
    IServiceProvider serviceProvider,
    TimeProvider timeProvider,
    ILogger<AttendanceOutboxDispatcher> logger) : BackgroundService
{
    private const int BatchSize = 20;

    private const int MaxAttempts = 5;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Attendance outbox dispatcher started.");

        using var timer = new PeriodicTimer(PollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in outbox dispatch loop. Will retry next tick.");
            }
        }

        logger.LogInformation("Attendance outbox dispatcher stopped.");
    }

    internal async Task DispatchBatchAsync(CancellationToken ct)
    {
        List<Guid> messageIds;
        var now = timeProvider.GetUtcNow();

        using (var queryScope = serviceProvider.CreateScope())
        {
            var db = queryScope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

            messageIds = await db.OutboxMessages
                .Where(m => m.ProcessedOn == null && m.NextAttemptOn <= now)
                .OrderBy(m => m.OccurredOn)
                .Take(BatchSize)
                .Select(m => m.Id)
                .ToListAsync(ct);
        }

        foreach (var messageId in messageIds)
        {
            await DispatchMessageAsync(messageId, ct);
        }
    }

    private async Task DispatchMessageAsync(Guid messageId, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        var now = timeProvider.GetUtcNow();

        var message = await db.OutboxMessages.FindAsync([messageId], ct);
        if (message is null || message.ProcessedOn is not null)
            return;

        var eventType = Type.GetType(message.Type);
        if (eventType is null)
        {
            logger.LogError(
                "Could not resolve type '{EventType}' for outbox message {MessageId}. Marking processed.",
                message.Type, message.Id);
            message.MarkProcessed(now);
            await db.SaveChangesAsync(ct);
            return;
        }

        object? payload;
        try
        {
            payload = JsonSerializer.Deserialize(message.Payload, eventType);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex,
                "Failed to deserialize outbox message {MessageId} of type '{EventType}'. Marking processed.",
                message.Id, message.Type);
            message.MarkProcessed(now);
            await db.SaveChangesAsync(ct);
            return;
        }

        if (payload is null)
        {
            logger.LogError(
                "Deserialized null payload for outbox message {MessageId}. Marking processed.",
                message.Id);
            message.MarkProcessed(now);
            await db.SaveChangesAsync(ct);
            return;
        }

        var handlerTypes = typeof(IEnumerable<>).MakeGenericType(typeof(IIntegrationEventHandler<>).MakeGenericType(eventType));
        var handlers = (IEnumerable<object>)scope.ServiceProvider.GetService(handlerTypes)!;

        if (handlers is null || !handlers.Any())
        {
            logger.LogWarning(
                "No handler registered for '{EventType}'. Marking message {MessageId} processed.",
                message.Type, message.Id);
            message.MarkProcessed(now);
            await db.SaveChangesAsync(ct);
            return;
        }

        try
        {
            foreach (var handler in handlers)
            {
                var handlerType = handler.GetType();
                var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<>.Handle))!;
                var task = (Task)method.Invoke(handler, [payload, ct])!;
                await task;
            }
            message.MarkProcessed(now);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Dispatched outbox message {MessageId} [{EventType}].", message.Id, message.Type);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            await HandleDispatchFailure(db, message, now, ex.InnerException, ct);
        }
        catch (Exception ex)
        {
            await HandleDispatchFailure(db, message, now, ex, ct);
        }
    }

    private async Task HandleDispatchFailure(
        AttendanceDbContext db,
        OutboxMessage message,
        DateTimeOffset now,
        Exception ex,
        CancellationToken ct)
    {
        var attempt = message.AttemptCount + 1;

        if (attempt >= MaxAttempts)
        {
            logger.LogError(ex,
                "Outbox message {MessageId} [{EventType}] failed after {MaxAttempts} attempts. Abandoning.",
                message.Id, message.Type, MaxAttempts);

            message.MarkAbandoned(now, $"Abandoned after {MaxAttempts} attempts. Last error: {ex.Message}");
            await db.SaveChangesAsync(ct);
            return;
        }

        logger.LogWarning(ex,
            "Outbox message {MessageId} [{EventType}] failed. Attempt {Attempt}/{MaxAttempts}. Next retry at {NextAttempt}.",
            message.Id, message.Type, attempt, MaxAttempts, now + RetryDelay);

        message.RecordFailure(now, now + RetryDelay, ex.Message);
        await db.SaveChangesAsync(ct);
    }
}