using VenuePass.BuildingBlocks.Application;
using Xunit;

namespace VenuePass.BuildingBlocks.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Ok_WhenCalled_ReturnsSuccessWithNoneError()
    {
        var result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Fail_WithValidationError_ReturnsFailure()
    {
        var error = Error.Validation("sample.error", "Failure message");
        
        var result = Result.Fail(error);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Fail_WithConflictError_SetsErrorProperties()
    {
        var error = Error.Conflict("sample.error", "Failure message");
        var result = Result.Fail(error);

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("sample.error", result.Error.Code);
        Assert.Equal("Failure message", result.Error.Message);
    }

    [Fact]
    public void Ok_Generic_WhenCalled_ReturnsValue()
    {
        var result = Result.Ok(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Value_WhenFail_ThrowsInvalidOperationException()
    {
        var error = Error.Validation("sample.error", "Failure message");
        var result = Result.Fail<int>(error);

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Fail_WithNoneTypeOrSuccessWithNonNone_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Fail(Error.None));
        Assert.Throws<ArgumentException>(() => Result.Fail<int>(Error.None));
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
