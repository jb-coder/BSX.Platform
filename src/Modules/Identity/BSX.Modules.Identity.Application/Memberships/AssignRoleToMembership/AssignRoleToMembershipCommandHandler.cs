using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
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

    /// <summary>Initializes a new instance of the <see cref="AssignRoleToMembershipCommandHandler"/> class.</summary>
    public AssignRoleToMembershipCommandHandler(
        ITenantMembershipRepository memberships,
        IRoleRepository roles,
        IUnitOfWork unitOfWork)
    {
        _memberships = memberships;
        _roles = roles;
        _unitOfWork = unitOfWork;
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

        Role? role = await _roles.GetByIdAsync(RoleId.From(request.RoleId), cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure(IdentityApplicationErrors.RoleNotFound);
        }

        if (!role.IsSystem && role.TenantId is not null && role.TenantId != membership.TenantId)
        {
            return Result.Failure(IdentityApplicationErrors.RoleTenantMismatch);
        }

        Result result = membership.AssignRole(role.Id);

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
