namespace VenuePass.BuildingBlocks.Extensions;

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using VenuePass.BuildingBlocks.Application;

public static class ErrorExtensions
{
    public static IResult ToProblemDetails(this Error error)
    {
        Dictionary<string, object?> extensions = new()
        {
            ["code"] = error.Code
        };

        if (error is ValidationError validationError)
        {
            extensions["details"] = validationError.Details.Select(x => new { field = x.Field, message = x.Message });
        }

        return error.Type switch
        {
            ErrorType.Validation => Results.Problem(
                title: "Validation error",
                detail: error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions),

            ErrorType.Forbidden => Results.Problem(
                title: "Forbidden",
                detail: error.Message,
                statusCode: StatusCodes.Status403Forbidden,
                extensions: extensions),

            ErrorType.Unauthorized => Results.Problem(
                title: "Unauthorized",
                detail: error.Message,
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: extensions),

            ErrorType.NotFound => Results.Problem(
                title: "Not found",
                detail: error.Message,
                statusCode: StatusCodes.Status404NotFound,
                extensions: extensions),

            ErrorType.Conflict => Results.Problem(
                title: "Conflict",
                detail: error.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: extensions),

            ErrorType.Concurrency => Results.Problem(
                title: "Concurrency conflict",
                detail: error.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: extensions),

            ErrorType.RateLimit => Results.Problem(
                title: "Rate limit exceeded",
                detail: error.Message,
                statusCode: StatusCodes.Status429TooManyRequests,
                extensions: extensions),

            ErrorType.Unavailable => Results.Problem(
                title: "Service unavailable",
                detail: error.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: extensions),

            ErrorType.Unexpected => Results.Problem(
                title: "Unexpected error",
                detail: error.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: extensions),

            ErrorType.None => Results.Problem(
                title: "Unexpected error",
                detail: "No error details were provided.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: extensions),

            _ => Results.Problem(
                title: "Unexpected error",
                detail: error.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: extensions)
        };
    }
}