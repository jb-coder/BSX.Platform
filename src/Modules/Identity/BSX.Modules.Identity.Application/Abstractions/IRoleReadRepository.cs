using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Read-model access for roles (CQRS read side). Implemented in Infrastructure.</summary>
public interface IRoleReadRepository
{
    /// <summary>Lists roles for a tenant; null lists system roles.</summary>
    /// <param name="tenantId">The tenant identifier; null for system roles.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<IReadOnlyList<RoleDto>> ListAsync(TenantId? tenantId, CancellationToken cancellationToken = default);
}
