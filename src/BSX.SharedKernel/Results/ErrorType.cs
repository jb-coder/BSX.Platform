namespace BSX.SharedKernel.Results;

/// <summary>
/// Classifies an <see cref="Error"/> so that a transport layer can map it to a response
/// without inspecting message strings.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// A generic, unexpected failure.
    /// </summary>
    Failure = 0,

    /// <summary>
    /// The request failed input validation.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// The requested resource could not be found.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// The request conflicts with the current state of the resource.
    /// </summary>
    Conflict = 3,

    /// <summary>
    /// The caller is not authenticated.
    /// </summary>
    Unauthorized = 4,

    /// <summary>
    /// The caller is authenticated but not permitted to perform the action.
    /// </summary>
    Forbidden = 5,

    /// <summary>
    /// An unexpected error occurred.
    /// </summary>
    Unexpected = 6,
}
