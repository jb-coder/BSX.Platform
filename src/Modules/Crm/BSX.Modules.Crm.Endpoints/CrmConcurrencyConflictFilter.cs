using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Application.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BSX.Modules.Crm.Endpoints;

/// <summary>
/// Translates an optimistic-concurrency failure raised by the unit of work into the platform's
/// RFC 9457 <c>409 Conflict</c> contract (ADR-011), so CRM clients receive the same problem shape
/// as every other expected failure instead of an HTTP 500.
/// </summary>
internal sealed class CrmConcurrencyConflictFilter : IEndpointFilter
{
    private readonly ILogger<CrmConcurrencyConflictFilter> _logger;

    /// <summary>Initializes a new instance of the <see cref="CrmConcurrencyConflictFilter"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public CrmConcurrencyConflictFilter(ILogger<CrmConcurrencyConflictFilter> logger) => _logger = logger;

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            return await next(context).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException exception)
        {
            _logger.LogWarning(
                exception,
                "CRM concurrency conflict for {Method} {Path}.",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path);

            return CrmResults.Problem(CrmApplicationErrors.ConcurrencyConflict);
        }
    }
}
