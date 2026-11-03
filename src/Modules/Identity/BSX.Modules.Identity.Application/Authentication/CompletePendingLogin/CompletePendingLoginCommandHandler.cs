using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication.CompletePendingLogin;

/// <summary>Handles <see cref="CompletePendingLoginCommand"/>.</summary>
public sealed class CompletePendingLoginCommandHandler
    : ICommandHandler<CompletePendingLoginCommand, AuthenticationTokensResponse>
{
    private readonly IPendingLoginStore _pendingLogins;
    private readonly IUserRepository _users;
    private readonly ITenantMembershipRepository _memberships;
    private readonly AuthenticationSessionIssuer _sessions;

    /// <summary>Initializes a new instance of the <see cref="CompletePendingLoginCommandHandler"/> class.</summary>
    /// <param name="pendingLogins">The server-side pending-login store.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="memberships">The membership repository.</param>
    /// <param name="sessions">The session issuer.</param>
    public CompletePendingLoginCommandHandler(
        IPendingLoginStore pendingLogins,
        IUserRepository users,
        ITenantMembershipRepository memberships,
        AuthenticationSessionIssuer sessions)
    {
        _pendingLogins = pendingLogins;
        _users = users;
        _memberships = memberships;
        _sessions = sessions;
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticationTokensResponse>> HandleAsync(
        CompletePendingLoginCommand request,
        CancellationToken cancellationToken = default)
    {
        // Single use: consume before any store access so a token cannot be replayed, even if the
        // selected tenant or account is no longer valid.
        PendingLogin? pendingLogin = _pendingLogins.Consume(request.PendingLoginToken);

        if (pendingLogin is null
            || !string.Equals(pendingLogin.SelectionId, request.SelectionId, StringComparison.Ordinal))
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.PendingLoginExpired);
        }

        if (!pendingLogin.Tenants.Any(tenant => tenant.TenantId == request.TenantId))
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidTenant);
        }

        User? user = await _users
            .GetByIdAsync(UserId.From(pendingLogin.UserId), cancellationToken)
            .ConfigureAwait(false);

        if (user is null || user.Status != UserStatus.Active)
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidCredentials);
        }

        TenantMembership? membership = await _memberships
            .FindAsync(TenantId.From(request.TenantId), user.Id, cancellationToken)
            .ConfigureAwait(false);

        if (membership is null || membership.Status != MembershipStatus.Active)
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.InvalidTenant);
        }

        return await _sessions
            .IssueAsync(user, membership, request.UserAgent, request.IpAddress, cancellationToken)
            .ConfigureAwait(false);
    }
}
