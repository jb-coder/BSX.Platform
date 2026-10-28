using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Infrastructure.Crm;
using BSX.SharedKernel.Results;

namespace BSX.Web.Sales;

/// <summary>
/// Maps optimistic-concurrency exceptions from in-process Sales dispatches to the same stable
/// result used by the HTTP endpoint filter. Interactive Blazor has no response to map.
/// </summary>
internal static class SalesWebResults
{
    private static readonly Error ConcurrencyConflict = Error.Conflict(
        "SALES.ConcurrencyConflict",
        "The resource was modified by another request. Reload it and retry.");

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
        catch (CrmReferenceValidationException exception)
        {
            return Result.Failure(
                SalesApplicationErrors.CrmReferenceNotFound(exception.ReferenceType, exception.ReferenceId));
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
        catch (CrmReferenceValidationException exception)
        {
            return Result.Failure<T>(
                SalesApplicationErrors.CrmReferenceNotFound(exception.ReferenceType, exception.ReferenceId));
        }
    }
}
