namespace BSX.SharedKernel.Results;

/// <summary>
/// Represents the outcome of an operation that does not produce a value.
/// Expected failures are returned as <see cref="Error"/> values instead of exceptions.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error when the operation failed; <see cref="Error.None"/> otherwise.</param>
    /// <exception cref="InvalidOperationException">Thrown when the success flag and the error are inconsistent.</exception>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error when the operation failed; otherwise <see cref="Error.None"/>.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a successful result carrying a value.
    /// </summary>
    /// <typeparam name="TValue">The type of the produced value.</typeparam>
    /// <param name="value">The produced value.</param>
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    /// <summary>
    /// Creates a failed result carrying a value type.
    /// </summary>
    /// <typeparam name="TValue">The type of the expected value.</typeparam>
    /// <param name="error">The error describing the failure.</param>
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}
