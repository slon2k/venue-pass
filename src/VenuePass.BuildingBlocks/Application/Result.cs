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

    public Error Error => _error ?? throw new InvalidOperationException("Failed result must contain an error.");

    public static Result Success() => new();

    public static Result Failure(Error error) => new(error);

    public static Result<T> Success<T>(T value) => new(value);

    public static Result<T> Failure<T>(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);

    public void Match(Action onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess();
        }
        else
        {
            onFailure(Error);
        }
    }

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure) => 
        IsSuccess ? onSuccess() : onFailure(Error);
}

public sealed class Result<T>
{
    private readonly T? _value;

    private readonly Error? _error;

    private readonly bool _isSuccess;

    public Result(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _value = value;
        _isSuccess = true;
        _error = null;
    }

    public Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        _error = error;
        _isSuccess = false;
        _value = default;
    }

    public bool IsSuccess => _isSuccess;

    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? throw new InvalidOperationException("Failed result must contain an error.");

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");
    
    public static implicit operator Result<T>(Error error) => new(error);

    public static implicit operator Result<T>(T value) => new(value);

    public void Match(Action<T> onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess(Value);
        }
        else
        {
            onFailure(Error);
        }
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure) => 
        IsSuccess ? onSuccess(Value) : onFailure(Error);
}
