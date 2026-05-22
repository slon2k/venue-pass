namespace VenuePass.BuildingBlocks.Presentation;

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using VenuePass.BuildingBlocks.Application;

public static class ErrorHttpMappingExtensions
{
    public static IResult ToProblemResult(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Dictionary<string, object?> extensions = new()
        {
            ["code"] = error.Code
        };

        if (error is ValidationError validationError)
        {
            extensions["details"] = validationError.Details
                .Select(x => new { field = x.Field, message = x.Message })
                .ToArray();
        }

        (string? title, int statusCode) = error.Type switch
        {
            ErrorType.Validation => ("Validation error", StatusCodes.Status400BadRequest),
            ErrorType.Forbidden => ("Forbidden", StatusCodes.Status403Forbidden),
            ErrorType.Unauthorized => ("Unauthorized", StatusCodes.Status401Unauthorized),
            ErrorType.NotFound => ("Not found", StatusCodes.Status404NotFound),
            ErrorType.Conflict => ("Conflict", StatusCodes.Status409Conflict),
            ErrorType.Concurrency => ("Concurrency conflict", StatusCodes.Status409Conflict),
            ErrorType.RateLimit => ("Rate limit exceeded", StatusCodes.Status429TooManyRequests),
            ErrorType.Unavailable => ("Service unavailable", StatusCodes.Status503ServiceUnavailable),
            ErrorType.Unexpected => ("Unexpected error", StatusCodes.Status500InternalServerError),
            _ => ("Unexpected error", StatusCodes.Status500InternalServerError)
        };

        return Results.Problem(
            title: title,
            detail: error.Message,
            statusCode: statusCode,
            extensions: extensions);
    }

    public static IResult ToProblem(Error error) => error.ToProblemResult();
}