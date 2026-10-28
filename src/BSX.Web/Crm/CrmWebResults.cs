using BSX.BuildingBlocks.Persistence;
using BSX.SharedKernel.Results;

namespace BSX.Web.Crm;

/// <summary>
/// Adapts in-process CRM dispatches to the web's Result-based error experience. The API maps
/// optimistic concurrency at the endpoint filter; interactive Blazor components have no HTTP
/// response to map, so the exception is converted to the same stable error value here.
/// </summary>
internal static class CrmWebResults
{
    private static readonly Error ConcurrencyConflict = Error.Conflict(
        "CRM.ConcurrencyConflict",
        "The record was modified by another request. Reload it and retry.");

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
