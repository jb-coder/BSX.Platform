using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Errors;
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

    /// <summary>Initializes a new instance of the <see cref="InviteUserToTenantCommandHandler"/> class.</summary>
    public InviteUserToTenantCommandHandler(
        IUserRepository users,
        ITenantMembershipRepository memberships,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _memberships = memberships;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<InviteUserToTenantResponse>> HandleAsync(InviteUserToTenantCommand request, CancellationToken cancellationToken = default)
    {
        if (request.TenantId == Guid.Empty)
        {
            return Result.Failure<InviteUserToTenantResponse>(IdentityApplicationErrors.InvalidTenantId);
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

        if (await _memberships.ExistsAsync(tenantId, user.Id, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<InviteUserToTenantResponse>(IdentityApplicationErrors.MembershipAlreadyExists);
        }

        UserId? invitedByUserId = request.InvitedByUserId is { } inviter && inviter != Guid.Empty
            ? UserId.From(inviter)
            : null;

        var membershipResult = TenantMembership.Invite(
            MembershipId.New(),
            tenantId,
            user.Id,
            invitedByUserId,
            _timeProvider.GetUtcNow());

        if (membershipResult.IsFailure)
        {
            return Result.Failure<InviteUserToTenantResponse>(membershipResult.Error);
        }

        await _memberships.AddAsync(membershipResult.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new InviteUserToTenantResponse(membershipResult.Value.Id.Value));
    }
}
