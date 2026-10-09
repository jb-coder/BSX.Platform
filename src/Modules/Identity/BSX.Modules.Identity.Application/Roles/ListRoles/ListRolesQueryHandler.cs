using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Roles.ListRoles;

/// <summary>Handles <see cref="ListRolesQuery"/>.</summary>
public sealed class ListRolesQueryHandler : IQueryHandler<ListRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IRoleReadRepository _roles;

    /// <summary>Initializes a new instance of the <see cref="ListRolesQueryHandler"/> class.</summary>
    public ListRolesQueryHandler(IRoleReadRepository roles) => _roles = roles;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoleDto>>> HandleAsync(ListRolesQuery request, CancellationToken cancellationToken = default)
    {
        TenantId? tenantId = null;

        if (request.TenantId.HasValue)
        {
            if (request.TenantId.Value == Guid.Empty)
            {
                return Result.Failure<IReadOnlyList<RoleDto>>(IdentityApplicationErrors.InvalidTenantId);
            }

            tenantId = TenantId.From(request.TenantId.Value);
        }

        IReadOnlyList<RoleDto> roles = await _roles.ListAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return Result.Success(roles);
    }
}
