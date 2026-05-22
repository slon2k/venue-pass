namespace VenuePass.BuildingBlocks.Application;

public record Error
{
    public ErrorType Type { get; }
    public string Code { get; }
    public string Message { get; }

    protected Error(ErrorType type, string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Type = type;
        Code = code;
        Message = message; 
    }
    
    public static Error Unexpected(string code, string message) => new(ErrorType.Unexpected, code, message);
    public static Error Validation(string code, string message) => new(ErrorType.Validation, code, message);
    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);
    public static Error Forbidden(string code, string message) => new(ErrorType.Forbidden, code, message);
    public static Error Unauthorized(string code, string message) => new(ErrorType.Unauthorized, code, message);
    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);
    public static Error Concurrency(string code, string message) => new(ErrorType.Concurrency, code, message);
    public static Error RateLimit(string code, string message) => new(ErrorType.RateLimit, code, message);
    public static Error Unavailable(string code, string message) => new(ErrorType.Unavailable, code, message);
}
