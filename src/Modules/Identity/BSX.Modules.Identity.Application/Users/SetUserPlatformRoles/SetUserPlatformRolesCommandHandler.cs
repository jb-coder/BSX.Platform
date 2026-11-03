using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Users.SetUserPlatformRoles;

/// <summary>Handles <see cref="SetUserPlatformRolesCommand"/>.</summary>
public sealed class SetUserPlatformRolesCommandHandler : ICommandHandler<SetUserPlatformRolesCommand>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="SetUserPlatformRolesCommandHandler"/> class.</summary>
    public SetUserPlatformRolesCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _users = users;
        _roles = roles;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        SetUserPlatformRolesCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidUserId);
        }

        // Platform role assignment is an administrative action over another global identity. An
        // administrator must never change their own roles, even through a direct application call.
        if (_currentUser.UserId is { } currentUserId && currentUserId == request.UserId)
        {
            return Result.Failure(IdentityApplicationErrors.SelfPlatformRoleChangeForbidden);
        }

        User? user = await _users
            .GetByIdAsync(UserId.From(request.UserId), cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(IdentityApplicationErrors.UserNotFound);
        }

        var requestedRoleIds = new HashSet<Guid>(request.RoleIds ?? []);

        if (requestedRoleIds.Contains(Guid.Empty))
        {
            return Result.Failure(IdentityApplicationErrors.InvalidRoleId);
        }

        foreach (Guid roleId in requestedRoleIds)
        {
            Role? role = await _roles
                .GetByIdAsync(RoleId.From(roleId), cancellationToken)
                .ConfigureAwait(false);

            if (role is null)
            {
                return Result.Failure(IdentityApplicationErrors.RoleNotFound);
            }

            // Platform roles are the roles with no tenant. Workspace roles belong to memberships.
            if (role.TenantId is not null)
            {
                return Result.Failure(IdentityApplicationErrors.RoleNotAssignableToPlatform);
            }
        }

        IReadOnlyCollection<Role> platformRoles = await _roles
            .ListByTenantAsync(tenantId: null, cancellationToken)
            .ConfigureAwait(false);

        Role? administratorRole = platformRoles.FirstOrDefault(role =>
            string.Equals(role.Name.Value, SystemRoleNames.Administrator, StringComparison.Ordinal));

        if (administratorRole is not null
            && user.Status == UserStatus.Active
            && user.PlatformRoleIds.Contains(administratorRole.Id)
            && !requestedRoleIds.Contains(administratorRole.Id.Value)
            && !await _users.ExistsOtherActiveUserWithPlatformRoleAsync(
                administratorRole.Id,
                user.Id,
                cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(IdentityApplicationErrors.LastAdministratorRequired);
        }

        var requestedRoleIdSet = new HashSet<RoleId>(requestedRoleIds.Select(RoleId.From));
        var currentRoleIdSet = new HashSet<RoleId>(user.PlatformRoleIds);

        if (currentRoleIdSet.SetEquals(requestedRoleIdSet))
        {
            // No change was required. Avoid opening a write transaction for an idempotent request.
            return Result.Success();
        }

        foreach (RoleId currentRoleId in currentRoleIdSet
            .Where(roleId => !requestedRoleIdSet.Contains(roleId))
            .ToArray())
        {
            Result remove = user.RemovePlatformRole(currentRoleId);

            if (remove.IsFailure)
            {
                return remove;
            }
        }

        foreach (RoleId requestedRoleId in requestedRoleIdSet
            .Where(roleId => !currentRoleIdSet.Contains(roleId)))
        {
            Result assign = user.AssignPlatformRole(requestedRoleId);

            if (assign.IsFailure)
            {
                return assign;
            }
        }

        await _users.TrackPlatformRoleChangesAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
