using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Memberships.AssignRoleToMembership;

/// <summary>Handles <see cref="AssignRoleToMembershipCommand"/>.</summary>
public sealed class AssignRoleToMembershipCommandHandler : ICommandHandler<AssignRoleToMembershipCommand>
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TenantGuard _tenantGuard;

    /// <summary>Initializes a new instance of the <see cref="AssignRoleToMembershipCommandHandler"/> class.</summary>
    public AssignRoleToMembershipCommandHandler(
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
    public async Task<Result> HandleAsync(AssignRoleToMembershipCommand request, CancellationToken cancellationToken = default)
    {
        if (request.MembershipId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidMembershipId);
        }

        if (request.RoleId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidRoleId);
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

        Role? role = await _roles.GetByIdAsync(RoleId.From(request.RoleId), cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure(IdentityApplicationErrors.RoleNotFound);
        }

        // Membership roles are always tenant roles. Platform roles are attached to the user account
        // by the platform administrator and must never be reachable through tenant membership
        // administration (ADR-020).
        if (role.IsSystem || role.TenantId is null)
        {
            return Result.Failure(IdentityApplicationErrors.RoleNotAssignableToMembership);
        }

        if (role.TenantId != membership.TenantId)
        {
            return Result.Failure(IdentityApplicationErrors.RoleTenantMismatch);
        }

        Result result = membership.AssignRole(role.Id);

        if (result.IsFailure)
        {
            return result;
        }

        await _memberships.TrackRoleChangesAsync(membership, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
