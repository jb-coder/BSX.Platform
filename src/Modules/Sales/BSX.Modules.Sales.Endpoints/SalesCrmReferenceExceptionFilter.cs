using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Infrastructure.Crm;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BSX.Modules.Sales.Endpoints;

/// <summary>
/// Maps an invalid CRM customer or organization reference to a controlled <c>409 Conflict</c>
/// problem response (ADR-011). The referenced resource is outside the Sales module, so a
/// referential-integrity failure is reported as a conflict rather than an unhandled 500.
/// </summary>
internal sealed class SalesCrmReferenceExceptionFilter : IEndpointFilter
{
    private readonly ILogger<SalesCrmReferenceExceptionFilter> _logger;

    /// <summary>Initializes a new instance of the <see cref="SalesCrmReferenceExceptionFilter"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public SalesCrmReferenceExceptionFilter(ILogger<SalesCrmReferenceExceptionFilter> logger) => _logger = logger;

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
        catch (CrmReferenceValidationException exception)
        {
            _logger.LogWarning(
                exception,
                "Sales rejected an invalid CRM {ReferenceType} reference for {Method} {Path}.",
                exception.ReferenceType,
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path);

            return SalesResults.Problem(
                SalesApplicationErrors.CrmReferenceNotFound(exception.ReferenceType, exception.ReferenceId));
        }
    }
}
