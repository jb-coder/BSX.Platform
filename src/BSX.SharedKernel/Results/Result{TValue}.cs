namespace BSX.SharedKernel.Results;

/// <summary>
/// Represents the outcome of an operation that produces a value on success.
/// </summary>
/// <typeparam name="TValue">The type of the produced value.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value)
        : base(true, Error.None)
        => _value = value;

    private Result(Error error)
        : base(false, error)
        => _value = default;

    /// <summary>
    /// Gets the produced value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>
    /// Creates a successful result carrying the supplied value.
    /// </summary>
    /// <param name="value">The produced value.</param>
    public static Result<TValue> Success(TValue value) => new(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    public static new Result<TValue> Failure(Error error) => new(error);

    /// <summary>
    /// Converts a value to a successful <see cref="Result{TValue}"/>.
    /// </summary>
    /// <param name="value">The produced value.</param>
    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
