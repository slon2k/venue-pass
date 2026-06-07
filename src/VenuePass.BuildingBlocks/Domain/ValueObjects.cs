namespace VenuePass.BuildingBlocks.Domain;

public readonly record struct DateTimeRange
{
    public DateTimeOffset? Start { get; }
    public DateTimeOffset? End { get; }

    public DateTimeRange(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start.HasValue && end.HasValue && start > end)
        {
            throw new ArgumentException("Start must be less than or equal to End.");
        }

        Start = start;
        End = end;
    }

    public bool Contains(DateTimeOffset dateTime)
        => (Start, End) switch
        {
            (null, null) => true,
            (null, var end) => dateTime <= end,
            (var start, null) => dateTime >= start,
            (var start, var end) => dateTime >= start && dateTime <= end
        };
}