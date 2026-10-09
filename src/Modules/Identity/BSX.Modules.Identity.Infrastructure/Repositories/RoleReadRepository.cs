using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.Repositories;

internal sealed class RoleReadRepository : IRoleReadRepository
{
    private readonly IdentityDbContext _context;

    public RoleReadRepository(IdentityDbContext context) => _context = context;

    public async Task<IReadOnlyList<RoleDto>> ListAsync(TenantId? tenantId, CancellationToken cancellationToken = default)
    {
        List<Role> roles = await _context.Roles
            .AsNoTracking()
            .Where(role => role.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return roles
            .Select(role => new RoleDto(
                role.Id.Value,
                role.TenantId?.Value,
                role.Name.Value,
                role.Description,
                role.IsSystem,
                role.Permissions.Select(permission => permission.Code).ToList()))
            .ToList();
    }
}
