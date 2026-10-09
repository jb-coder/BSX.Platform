using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Roles.SetRolePermissions;

/// <summary>Handles <see cref="SetRolePermissionsCommand"/>.</summary>
public sealed class SetRolePermissionsCommandHandler : ICommandHandler<SetRolePermissionsCommand>
{
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of the <see cref="SetRolePermissionsCommandHandler"/> class.</summary>
    public SetRolePermissionsCommandHandler(IRoleRepository roles, IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(SetRolePermissionsCommand request, CancellationToken cancellationToken = default)
    {
        if (request.RoleId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidRoleId);
        }

        var permissionsResult = PermissionCodes.Parse(request.Permissions);

        if (permissionsResult.IsFailure)
        {
            return Result.Failure(permissionsResult.Error);
        }

        Role? role = await _roles.GetByIdAsync(RoleId.From(request.RoleId), cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure(IdentityApplicationErrors.RoleNotFound);
        }

        HashSet<Permission> target = permissionsResult.Value.ToHashSet();

        foreach (Permission permission in role.Permissions)
        {
            if (!target.Contains(permission))
            {
                Result revoke = role.Revoke(permission);

                if (revoke.IsFailure)
                {
                    return revoke;
                }
            }
        }

        foreach (Permission permission in target)
        {
            if (!role.Permissions.Contains(permission))
            {
                Result grant = role.Grant(permission);

                if (grant.IsFailure)
                {
                    return grant;
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
