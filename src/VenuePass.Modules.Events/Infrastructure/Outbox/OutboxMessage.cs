namespace VenuePass.Modules.Events.Infrastructure.Outbox;
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }

    public DateTimeOffset OccurredOn { get; private set; }

    public string Type { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public int AttemptCount { get; private set; }

    public DateTimeOffset? LastAttemptedOn { get; private set; }

    public DateTimeOffset? NextAttemptOn { get; private set; }

    public DateTimeOffset? ProcessedOn { get; private set; }

    public string? Error { get; private set; }

    private OutboxMessage() { }

    private OutboxMessage(
        Guid id,
        DateTimeOffset occurredOn,
        string type,
        string payload)
    {
        Id = id;
        OccurredOn = occurredOn;
        Type = type;
        Payload = payload;
        AttemptCount = 0;
        NextAttemptOn = occurredOn;
    }

    public static OutboxMessage Create(
        DateTimeOffset occurredOn,
        string type,
        string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage(Guid.CreateVersion7(), occurredOn, type, payload);
    }

    public void MarkProcessed(DateTimeOffset processedOn)
    {
        ProcessedOn = processedOn;
        Error = null;
    }

    public void RecordFailure(
        DateTimeOffset attemptedOn,
        DateTimeOffset nextAttemptOn,
        string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        AttemptCount++;
        LastAttemptedOn = attemptedOn;
        NextAttemptOn = nextAttemptOn;
        Error = error;
    }

    public void MarkAttempted(DateTimeOffset attemptedOn)
    {
        AttemptCount++;
        LastAttemptedOn = attemptedOn;
        Error = null;
    }
}