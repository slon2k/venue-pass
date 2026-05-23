namespace VenuePass.BuildingBlocks.Domain;

public readonly record struct DomainError
{
    public string Code { get; }
    public string Message { get; }

    public DomainError(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
    }
}