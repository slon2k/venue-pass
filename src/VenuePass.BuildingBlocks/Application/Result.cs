namespace VenuePass.BuildingBlocks.Application;

public sealed class Result
{
    private readonly Error? _error;

    private readonly bool _isSuccess;

    private Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _isSuccess = false;
        _error = error;
    }

    private Result()
    {
        _isSuccess = true;
        _error = null;
    }

    public bool IsSuccess => _isSuccess;

    public bool IsFailure => !IsSuccess;

    public Error Error => IsFailure
        ? _error ?? throw new InvalidOperationException("Failed result must contain an error.")
        : throw new InvalidOperationException("Cannot access error of a successful result.");

    public static Result Success() => new();

    public static Result Failure(Error error) => new(error);

    public static Result<T> Success<T>(T value) => value;

    public static Result<T> Failure<T>(Error error) => error;

    public static implicit operator Result(Error error) => Failure(error);

    public void Match(Action onSuccess, Action<Error> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        if (IsSuccess)
        {
            onSuccess();
        }
        else
        {
            onFailure(Error);
        }
    }

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess() : onFailure(Error);
    }
}

public sealed class Result<T>
{
    private readonly T? _value;

    private readonly Error? _error;

    private readonly bool _isSuccess;

    private Result(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _value = value;
        _isSuccess = true;
        _error = null;
    }

    private Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        _error = error;
        _isSuccess = false;
        _value = default;
    }

    public bool IsSuccess => _isSuccess;

    public bool IsFailure => !IsSuccess;

    public Error Error => IsFailure
        ? _error ?? throw new InvalidOperationException("Failed result must contain an error.")
        : throw new InvalidOperationException("Cannot access error of a successful result.");

    public T Value => IsSuccess
        ? _value ?? throw new InvalidOperationException("Successful result must contain a value.")
        : throw new InvalidOperationException("Cannot access value of a failed result.");
    
    public static implicit operator Result<T>(Error error) => new(error);

    public static implicit operator Result<T>(T value) => new(value);

    public void Match(Action<T> onSuccess, Action<Error> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        if (IsSuccess)
        {
            onSuccess(Value);
        }
        else
        {
            onFailure(Error);
        }
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}
