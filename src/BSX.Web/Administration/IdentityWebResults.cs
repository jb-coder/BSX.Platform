using BSX.BuildingBlocks.Persistence;
using BSX.SharedKernel.Results;

namespace BSX.Web.Administration;

/// <summary>Maps optimistic-concurrency exceptions to the administration conflict experience.</summary>
internal static class IdentityWebResults
{
    private static readonly Error ConcurrencyConflict = Error.Conflict(
        "Identity.Concurrency.Conflict",
        "A concurrent modification was detected. Reload and retry.");

    /// <summary>Executes a command and maps optimistic-concurrency failures to a Result.</summary>
    public static async Task<Result> ExecuteAsync(Func<Task<Result>> operation)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(ConcurrencyConflict);
        }
    }

    /// <summary>Executes a command with a value and maps optimistic-concurrency failures to a Result.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> operation)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<T>(ConcurrencyConflict);
        }
    }
}
