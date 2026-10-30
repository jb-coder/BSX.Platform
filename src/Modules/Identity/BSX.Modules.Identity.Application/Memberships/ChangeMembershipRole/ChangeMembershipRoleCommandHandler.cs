using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Memberships.ChangeMembershipRole;

/// <summary>Handles <see cref="ChangeMembershipRoleCommand"/>.</summary>
public sealed class ChangeMembershipRoleCommandHandler : ICommandHandler<ChangeMembershipRoleCommand>
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TenantGuard _tenantGuard;

    /// <summary>Initializes a new instance of the <see cref="ChangeMembershipRoleCommandHandler"/> class.</summary>
    public ChangeMembershipRoleCommandHandler(
        ITenantMembershipRepository memberships,
        IRoleRepository roles,
        IUnitOfWork unitOfWork,
        TenantGuard tenantGuard)
    {
        _memberships = memberships;
        _roles = roles;
        _unitOfWork = unitOfWork;
        _tenantGuard = tenantGuard;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        ChangeMembershipRoleCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.MembershipId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidMembershipId);
        }

        TenantMembership? membership = await _memberships
            .GetByIdAsync(MembershipId.From(request.MembershipId), cancellationToken)
            .ConfigureAwait(false);

        if (membership is null)
        {
            return Result.Failure(IdentityApplicationErrors.MembershipNotFound);
        }

        Result guard = _tenantGuard.Ensure(membership.TenantId.Value);
        if (guard.IsFailure)
        {
            return guard;
        }

        Role? role = null;
        if (request.RoleId is { } roleValue)
        {
            if (roleValue == Guid.Empty)
            {
                return Result.Failure(IdentityApplicationErrors.InvalidRoleId);
            }

            role = await _roles
                .GetByIdAsync(RoleId.From(roleValue), cancellationToken)
                .ConfigureAwait(false);

            if (role is null)
            {
                return Result.Failure(IdentityApplicationErrors.RoleNotFound);
            }

            // Membership roles are always tenant roles; platform roles are never assignable here
            // (ADR-020).
            if (role.IsSystem || role.TenantId is null)
            {
                return Result.Failure(IdentityApplicationErrors.RoleNotAssignableToMembership);
            }

            if (role.TenantId != membership.TenantId)
            {
                return Result.Failure(IdentityApplicationErrors.RoleTenantMismatch);
            }
        }

        foreach (RoleId currentRole in membership.RoleIds.ToArray())
        {
            Result remove = membership.RemoveRole(currentRole);
            if (remove.IsFailure)
            {
                return remove;
            }
        }

        if (role is not null)
        {
            Result assign = membership.AssignRole(role.Id);
            if (assign.IsFailure)
            {
                return assign;
            }
        }

        await _memberships.TrackRoleChangesAsync(membership, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
