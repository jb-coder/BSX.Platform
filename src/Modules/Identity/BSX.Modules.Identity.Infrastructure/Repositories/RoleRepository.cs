using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context) => _context = context;

    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
        => _context.Roles.FirstOrDefaultAsync(role => role.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(TenantId? tenantId, RoleName name, CancellationToken cancellationToken = default)
        => _context.Roles.AnyAsync(role => role.Name == name && role.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyCollection<Role>> ListByTenantAsync(TenantId? tenantId, CancellationToken cancellationToken = default)
    {
        List<Role> roles = await _context.Roles
            .Where(role => role.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return roles;
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        => await _context.Roles.AddAsync(role, cancellationToken).ConfigureAwait(false);
}
