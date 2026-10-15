using BSX.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace BSX.Modules.Identity.Endpoints;

/// <summary>Maps <see cref="Result"/> outcomes to HTTP responses (ADR-011).</summary>
public static class IdentityResults
{
    /// <summary>Maps a result without a value.</summary>
    /// <param name="result">The result.</param>
    public static IResult From(Result result)
        => result.IsSuccess ? TypedResults.NoContent() : Problem(result.Error);

    /// <summary>Maps a result with a value.</summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="result">The result.</param>
    public static IResult From<TValue>(Result<TValue> result)
        => result.IsSuccess ? TypedResults.Ok(result.Value) : Problem(result.Error);

    /// <summary>Maps a result with a value to 201 Created.</summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="result">The result.</param>
    public static IResult FromCreated<TValue>(Result<TValue> result)
        => result.IsSuccess ? TypedResults.Created(string.Empty, result.Value) : Problem(result.Error);

    private static IResult Problem(Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        return TypedResults.Problem(statusCode: statusCode, title: error.Code, detail: error.Message);
    }
}
