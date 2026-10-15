namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// Raised by a unit of work when a concurrent modification is detected (optimistic concurrency).
/// The caller may re-read the aggregate and retry, or surface a controlled conflict.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.</summary>
    /// <param name="message">The message.</param>
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
