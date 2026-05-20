namespace VenuePass.BuildingBlocks.Application;

public sealed class Result
{
    private Result(bool isSuccess, Error error)
    {
        if (isSuccess && error.Type != ErrorType.None)
        {
            throw new ArgumentException("Successful result must use Error.None.", nameof(error));
        }

        if (!isSuccess && error.Type == ErrorType.None)
        {
            throw new ArgumentException("Failed result must provide an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Ok() => new(true, Error.None);

    public static Result Fail(Error error) => new(false, error);

    public static Result<T> Ok<T>(T value) => new(value, true, Error.None);

    public static Result<T> Fail<T>(Error error) => new(default, false, error);

    public static implicit operator Result(Error error) => Fail(error);

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

    internal Result(T? value, bool isSuccess, Error error)
    {
        if (isSuccess && value is null)
        {
            throw new ArgumentException("Successful result must provide a value.", nameof(value));
        }

        if (isSuccess && error.Type != ErrorType.None)
        {
            throw new ArgumentException("Successful result must use Error.None.", nameof(error));
        }

        if (!isSuccess && error.Type == ErrorType.None)
        {
            throw new ArgumentException("Failed result must provide an error.", nameof(error));
        }

        _value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");
    
    public static implicit operator Result<T>(Error error) => new(default, false, error);

    public static implicit operator Result<T>(T value) => new(value, true, Error.None);

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
