using BSX.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace BSX.Modules.Crm.Endpoints;

/// <summary>Maps CRM <see cref="Result"/> outcomes to HTTP responses (ADR-011).</summary>
public static class CrmResults
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

    /// <summary>Maps a created identifier to 201 with a Location header.</summary>
    /// <param name="result">The result carrying the new identifier.</param>
    /// <param name="locationPrefix">The resource location prefix.</param>
    public static IResult FromCreated(Result<Guid> result, string locationPrefix)
        => result.IsSuccess
            ? TypedResults.Created($"{locationPrefix}/{result.Value}", result.Value)
            : Problem(result.Error);

    /// <summary>Maps a CRM error to an RFC 9457 problem response.</summary>
    /// <param name="error">The error.</param>
    public static IResult Problem(Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        return TypedResults.Problem(statusCode: statusCode, title: error.Code, detail: error.Message);
    }
}
