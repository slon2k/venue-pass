using VenuePass.BuildingBlocks.Application;
using Xunit;

namespace VenuePass.BuildingBlocks.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_WhenCalled_ReturnsSuccess()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Error_WhenSuccess_ThrowsInvalidOperationException()
    {
        var result = Result.Success();

        void Act() => _ = result.Error;

        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Failure_WithValidationError_ReturnsFailure()
    {
        var error = Error.Validation("sample.error", "Failure message");
        
        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Failure_WithConflictError_SetsErrorProperties()
    {
        var error = Error.Conflict("sample.error", "Failure message");
        var result = Result.Failure(error);

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("sample.error", result.Error.Code);
        Assert.Equal("Failure message", result.Error.Message);
    }

    [Fact]
    public void Success_Generic_WhenCalled_ReturnsValue()
    {
        var result = Result.Success(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Value_WhenFailure_ThrowsInvalidOperationException()
    {
        var error = Error.Validation("sample.error", "Failure message");
        var result = Result.Failure<int>(error);

        void Act() => _ = result.Value;

        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Failure_WithNullError_ThrowsArgumentNullException()
    {
        void Act1() => _ = Result.Failure((Error)null!);
        void Act2() => _ = Result.Failure<string>((Error)null!);

        Assert.Throws<ArgumentNullException>(Act1);
        Assert.Throws<ArgumentNullException>(Act2);
    }

    [Fact]
    public void Success_Generic_WithNullValue_ThrowsArgumentNullException()
    {
        void Act() => _ = Result.Success<string>(null!);

        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void ValidationError_WhenConstructed_DetailsAreImmutableCopy()
    {
        var details = new List<ValidationErrorDetail>
        {
            new("name", "Name is required")
        };

        var error = new ValidationError("validation.failed", "Validation failed", details);

        details.Add(new ValidationErrorDetail("date", "Date is invalid"));

        Assert.Single(error.Details);
    }

    [Fact]
    public void ErrorType_Enum_ContainsAllRoutingCases()
    {
        Assert.Contains(ErrorType.Validation, Enum.GetValues<ErrorType>());
        Assert.Contains(ErrorType.Conflict, Enum.GetValues<ErrorType>());
        Assert.Contains(ErrorType.Forbidden, Enum.GetValues<ErrorType>());
        Assert.Contains(ErrorType.Unauthorized, Enum.GetValues<ErrorType>());
        Assert.Contains(ErrorType.NotFound, Enum.GetValues<ErrorType>());
        Assert.Contains(ErrorType.Concurrency, Enum.GetValues<ErrorType>());
        Assert.Contains(ErrorType.RateLimit, Enum.GetValues<ErrorType>());
        Assert.Contains(ErrorType.Unavailable, Enum.GetValues<ErrorType>());
        Assert.Contains(ErrorType.Unexpected, Enum.GetValues<ErrorType>());
    }
}
