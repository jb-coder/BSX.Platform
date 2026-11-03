using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Memberships.RemoveMembership;

/// <summary>Handles <see cref="RemoveMembershipCommand"/>.</summary>
public sealed class RemoveMembershipCommandHandler : ICommandHandler<RemoveMembershipCommand>
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUserSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TenantGuard _tenantGuard;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="RemoveMembershipCommandHandler"/> class.</summary>
    public RemoveMembershipCommandHandler(
        ITenantMembershipRepository memberships,
        IUserSessionRepository sessions,
        IUnitOfWork unitOfWork,
        TenantGuard tenantGuard,
        TimeProvider timeProvider)
    {
        _memberships = memberships;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _tenantGuard = tenantGuard;
        _timeProvider = timeProvider;
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

        // Removing a member also ends every session bound to that tenant. The revocation is part of
        // the same unit of work as the membership change, so a successful command cannot leave a
        // usable refresh-token chain behind.
        IReadOnlyCollection<UserSession> sessions = await _sessions
            .ListActiveByUserAsync(membership.UserId, cancellationToken)
            .ConfigureAwait(false);

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

        foreach (UserSession session in sessions.Where(session => session.TenantId == membership.TenantId))
        {
            session.Revoke(nowUtc, "membership_removed");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
