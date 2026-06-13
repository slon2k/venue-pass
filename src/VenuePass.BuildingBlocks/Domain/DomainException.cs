namespace VenuePass.BuildingBlocks.Domain;

public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message) : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
    }

    protected DomainException(DomainError error) : base(error.Message)
    {
        Code = error.Code;
    }
}

public sealed class DomainRuleViolationException(DomainError error) : DomainException(error)
{
}

public sealed class DomainConflictException(DomainError error) : DomainException(error)
{
}

public sealed class DomainNotFoundException(DomainError error) : DomainException(error)
{
}