namespace BSX.SharedKernel.Results;

/// <summary>
/// Represents an expected failure.
/// Business errors should be modelled as values, not thrown as exceptions.
/// </summary>
/// <param name="Code">A stable, machine-readable error code.</param>
/// <param name="Message">A human-readable description of the error.</param>
/// <param name="Type">The classification of the error.</param>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>
    /// Represents the absence of an error. Used by successful results.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Creates a generic failure error.
    /// </summary>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>
    /// Creates a not-found error.
    /// </summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>
    /// Creates an unexpected error.
    /// </summary>
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
