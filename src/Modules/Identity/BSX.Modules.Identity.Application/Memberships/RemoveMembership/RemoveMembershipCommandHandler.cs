using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Memberships.RemoveMembership;

/// <summary>Handles <see cref="RemoveMembershipCommand"/>.</summary>
public sealed class RemoveMembershipCommandHandler : ICommandHandler<RemoveMembershipCommand>
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TenantGuard _tenantGuard;

    /// <summary>Initializes a new instance of the <see cref="RemoveMembershipCommandHandler"/> class.</summary>
    public RemoveMembershipCommandHandler(
        ITenantMembershipRepository memberships,
        IUnitOfWork unitOfWork,
        TenantGuard tenantGuard)
    {
        _memberships = memberships;
        _unitOfWork = unitOfWork;
        _tenantGuard = tenantGuard;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(RemoveMembershipCommand request, CancellationToken cancellationToken = default)
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

        Result result = membership.Remove();
        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
