namespace VenuePass.BuildingBlocks.Application;


public sealed record ValidationError : Error
{
    public ValidationError(
        string code,
        string message,
        IReadOnlyList<ValidationErrorDetail> details)
        : base(ErrorType.Validation, code, message)
    {
        Details = [.. details];
    }

    public static ValidationError Create(string code, string message, IReadOnlyList<ValidationErrorDetail> details) =>
        new(code, message, details);

    public IReadOnlyList<ValidationErrorDetail> Details { get; }
}

public sealed record ValidationErrorDetail(string Field, string Message);