using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Application.Mappings;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Roles.GetRoleById;

/// <summary>Handles <see cref="GetRoleByIdQuery"/>.</summary>
public sealed class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, RoleDto>
{
    private readonly IRoleRepository _roles;
    private readonly TenantGuard _tenantGuard;

    /// <summary>Initializes a new instance of the <see cref="GetRoleByIdQueryHandler"/> class.</summary>
    public GetRoleByIdQueryHandler(IRoleRepository roles, TenantGuard tenantGuard)
    {
        _roles = roles;
        _tenantGuard = tenantGuard;
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> HandleAsync(GetRoleByIdQuery request, CancellationToken cancellationToken = default)
    {
        if (request.RoleId == Guid.Empty)
        {
            return Result.Failure<RoleDto>(IdentityApplicationErrors.InvalidRoleId);
        }

        Role? role = await _roles.GetByIdAsync(RoleId.From(request.RoleId), cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure<RoleDto>(IdentityApplicationErrors.RoleNotFound);
        }

        // Platform/system roles are shared read-only definitions. A tenant role, however, may only
        // be read by a principal whose active tenant owns it. This prevents an ID-based role-detail
        // lookup from crossing a tenant boundary.
        if (role.TenantId is { } roleTenant)
        {
            Result tenantGuard = _tenantGuard.Ensure(roleTenant.Value);
            if (tenantGuard.IsFailure)
            {
                return Result.Failure<RoleDto>(tenantGuard.Error);
            }
        }

        return Result.Success(role.ToDto());
    }
}
