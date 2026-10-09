using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Errors;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.Memberships;

/// <summary>
/// Links a global user to a tenant and owns the tenant-scoped role assignments (ADR-013).
/// </summary>
public sealed class TenantMembership : AggregateRoot<MembershipId>
{
    private readonly HashSet<RoleId> _roleIds = [];

    private TenantMembership(
        MembershipId id,
        TenantId tenantId,
        UserId userId,
        UserId? invitedByUserId,
        DateTimeOffset invitedOnUtc)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        InvitedByUserId = invitedByUserId;
        InvitedOnUtc = invitedOnUtc;
        Status = MembershipStatus.Invited;
    }

    private TenantMembership()
    {
    }

    /// <summary>Gets the tenant identifier.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Gets the user identifier.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Gets the membership status.</summary>
    public MembershipStatus Status { get; private set; }

    /// <summary>Gets the inviter, if any.</summary>
    public UserId? InvitedByUserId { get; private set; }

    /// <summary>Gets the invitation timestamp.</summary>
    public DateTimeOffset InvitedOnUtc { get; private set; }

    /// <summary>Gets the acceptance timestamp, if accepted.</summary>
    public DateTimeOffset? JoinedOnUtc { get; private set; }

    /// <summary>Gets the suspension timestamp, if suspended.</summary>
    public DateTimeOffset? SuspendedOnUtc { get; private set; }

    /// <summary>Gets the assigned tenant role identifiers.</summary>
    public IReadOnlyCollection<RoleId> RoleIds => _roleIds.ToArray();

    /// <summary>Invites a user to a tenant.</summary>
    /// <param name="id">The membership identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="invitedByUserId">The inviter, if any.</param>
    /// <param name="invitedOnUtc">The invitation timestamp.</param>
    public static Result<TenantMembership> Invite(
        MembershipId id,
        TenantId tenantId,
        UserId userId,
        UserId? invitedByUserId,
        DateTimeOffset invitedOnUtc)
    {
        var membership = new TenantMembership(id, tenantId, userId, invitedByUserId, invitedOnUtc);
        membership.RaiseDomainEvent(new MembershipInvited(id, tenantId, userId));
        return Result.Success(membership);
    }

    /// <summary>Accepts the invitation and activates the membership.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result Accept(DateTimeOffset nowUtc)
    {
        if (Status != MembershipStatus.Invited)
        {
            return Result.Failure(IdentityErrors.MembershipInvalidTransition);
        }

        Status = MembershipStatus.Active;
        JoinedOnUtc = nowUtc;
        RaiseDomainEvent(new MembershipActivated(Id, TenantId, UserId));
        return Result.Success();
    }

    /// <summary>Suspends an active membership.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result Suspend(DateTimeOffset nowUtc)
    {
        if (Status != MembershipStatus.Active)
        {
            return Result.Failure(IdentityErrors.MembershipInvalidTransition);
        }

        Status = MembershipStatus.Suspended;
        SuspendedOnUtc = nowUtc;
        RaiseDomainEvent(new MembershipSuspended(Id, TenantId, UserId));
        return Result.Success();
    }

    /// <summary>Reactivates a suspended membership.</summary>
    public Result Reactivate()
    {
        if (Status != MembershipStatus.Suspended)
        {
            return Result.Failure(IdentityErrors.MembershipInvalidTransition);
        }

        Status = MembershipStatus.Active;
        SuspendedOnUtc = null;
        RaiseDomainEvent(new MembershipReactivated(Id, TenantId, UserId));
        return Result.Success();
    }

    /// <summary>Removes the membership from the tenant.</summary>
    public Result Remove()
    {
        if (Status == MembershipStatus.Removed)
        {
            return Result.Failure(IdentityErrors.MembershipInvalidTransition);
        }

        Status = MembershipStatus.Removed;
        _roleIds.Clear();
        RaiseDomainEvent(new MembershipRemoved(Id, TenantId, UserId));
        return Result.Success();
    }

    /// <summary>Assigns a tenant role to the membership.</summary>
    /// <param name="roleId">The role identifier.</param>
    public Result AssignRole(RoleId roleId)
    {
        if (Status == MembershipStatus.Removed)
        {
            return Result.Failure(IdentityErrors.MembershipInvalidTransition);
        }

        if (!_roleIds.Add(roleId))
        {
            return Result.Failure(IdentityErrors.MembershipRoleAlreadyAssigned);
        }

        RaiseDomainEvent(new MembershipRoleAssigned(Id, TenantId, UserId, roleId));
        return Result.Success();
    }

    /// <summary>Removes a tenant role from the membership.</summary>
    /// <param name="roleId">The role identifier.</param>
    public Result RemoveRole(RoleId roleId)
    {
        if (!_roleIds.Remove(roleId))
        {
            return Result.Failure(IdentityErrors.MembershipRoleNotAssigned);
        }

        RaiseDomainEvent(new MembershipRoleRemoved(Id, TenantId, UserId, roleId));
        return Result.Success();
    }
}
