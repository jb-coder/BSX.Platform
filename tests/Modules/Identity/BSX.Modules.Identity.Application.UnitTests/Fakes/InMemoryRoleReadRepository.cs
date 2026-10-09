using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>In-memory <see cref="IRoleReadRepository"/> for query tests.</summary>
internal sealed class InMemoryRoleReadRepository : IRoleReadRepository
{
    private readonly List<RoleDto> _roles = [];

    public void Seed(params RoleDto[] roles) => _roles.AddRange(roles);

    public Task<IReadOnlyList<RoleDto>> ListAsync(TenantId? tenantId, CancellationToken cancellationToken = default)
    {
        Guid? tenant = tenantId?.Value;
        IReadOnlyList<RoleDto> roles = _roles.Where(role => role.TenantId == tenant).ToList();
        return Task.FromResult(roles);
    }
}
