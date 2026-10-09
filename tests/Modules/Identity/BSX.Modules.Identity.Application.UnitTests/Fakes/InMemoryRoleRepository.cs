using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>In-memory <see cref="IRoleRepository"/> for handler tests.</summary>
internal sealed class InMemoryRoleRepository : IRoleRepository
{
    private readonly List<Role> _roles = [];

    public IReadOnlyCollection<Role> Roles => _roles;

    public void Seed(params Role[] roles) => _roles.AddRange(roles);

    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_roles.Find(role => role.Id == id));

    public Task<bool> ExistsByNameAsync(TenantId? tenantId, RoleName name, CancellationToken cancellationToken = default)
        => Task.FromResult(_roles.Exists(role => role.TenantId == tenantId && role.Name == name));

    public Task<IReadOnlyCollection<Role>> ListByTenantAsync(TenantId? tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<Role>>(_roles.Where(role => role.TenantId == tenantId).ToList());

    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        _roles.Add(role);
        return Task.CompletedTask;
    }
}
