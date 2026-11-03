using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Memberships.InviteUserToTenant;

/// <summary>Handles <see cref="InviteUserToTenantCommand"/>.</summary>
public sealed class InviteUserToTenantCommandHandler : ICommandHandler<InviteUserToTenantCommand, InviteUserToTenantResponse>
{
    private readonly IUserRepository _users;
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly TenantGuard _tenantGuard;
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="InviteUserToTenantCommandHandler"/> class.</summary>
    public InviteUserToTenantCommandHandler(
        IUserRepository users,
        ITenantMembershipRepository memberships,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        TenantGuard tenantGuard,
        ICurrentUser currentUser)
    {
        _users = users;
        _memberships = memberships;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _tenantGuard = tenantGuard;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<InviteUserToTenantResponse>> HandleAsync(InviteUserToTenantCommand request, CancellationToken cancellationToken = default)
    {
        Result guard = _tenantGuard.Ensure(request.TenantId);
        if (guard.IsFailure)
        {
            return Result.Failure<InviteUserToTenantResponse>(guard.Error);
        }

        if (request.UserId == Guid.Empty)
        {
            return Result.Failure<InviteUserToTenantResponse>(IdentityApplicationErrors.InvalidUserId);
        }

        var tenantId = TenantId.From(request.TenantId);

        User? user = await _users.GetByIdAsync(UserId.From(request.UserId), cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<InviteUserToTenantResponse>(IdentityApplicationErrors.UserNotFound);
        }

        TenantMembership? existing = await _memberships
            .FindAsync(tenantId, user.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null && existing.Status != MembershipStatus.Removed)
        {
            return Result.Failure<InviteUserToTenantResponse>(IdentityApplicationErrors.MembershipAlreadyExists);
        }

        // Provenance is derived from the authenticated principal. The request field is retained
        // only for wire compatibility and is deliberately ignored so a tenant administrator cannot
        // attribute an invitation to another user.
        UserId? invitedByUserId = _currentUser.UserId is { } inviter && inviter != Guid.Empty
            ? UserId.From(inviter)
            : null;

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        TenantMembership membership;

        if (existing is not null)
        {
            // A removed membership is re-invited in place. The existing unique (tenant, user) row is
            // reused, roles are cleared and a fresh acceptance is required.
            Result reinvite = existing.Reinvite(invitedByUserId, nowUtc);
            if (reinvite.IsFailure)
            {
                return Result.Failure<InviteUserToTenantResponse>(reinvite.Error);
            }

            membership = existing;
        }
        else
        {
            var membershipResult = TenantMembership.Invite(
                MembershipId.New(),
                tenantId,
                user.Id,
                invitedByUserId,
                nowUtc);

            if (membershipResult.IsFailure)
            {
                return Result.Failure<InviteUserToTenantResponse>(membershipResult.Error);
            }

            membership = membershipResult.Value;
            await _memberships.AddAsync(membership, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new InviteUserToTenantResponse(membership.Id.Value));
    }
}
