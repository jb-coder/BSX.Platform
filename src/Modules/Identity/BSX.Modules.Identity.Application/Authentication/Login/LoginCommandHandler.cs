using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication.Login;

/// <summary>Handles <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthenticationTokensResponse>
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly CredentialAuthenticator _authenticator;
    private readonly AuthenticationSessionIssuer _sessions;
    private readonly IRateLimiter _rateLimiter;
    private readonly RateLimitOptions _rateLimitOptions;

    /// <summary>Initializes a new instance of the <see cref="LoginCommandHandler"/> class.</summary>
    /// <param name="memberships">The membership repository.</param>
    /// <param name="authenticator">The credential authenticator.</param>
    /// <param name="sessions">The authenticated-session issuer.</param>
    /// <param name="rateLimiter">The authentication rate limiter.</param>
    /// <param name="rateLimitOptions">The rate-limit policy.</param>
    public LoginCommandHandler(
        ITenantMembershipRepository memberships,
        CredentialAuthenticator authenticator,
        AuthenticationSessionIssuer sessions,
        IRateLimiter rateLimiter,
        RateLimitOptions rateLimitOptions)
    {
        _memberships = memberships;
        _authenticator = authenticator;
        _sessions = sessions;
        _rateLimiter = rateLimiter;
        _rateLimitOptions = rateLimitOptions;
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticationTokensResponse>> HandleAsync(
        LoginCommand request,
        CancellationToken cancellationToken = default)
    {
        string partitionKey = $"login:{request.Email?.Trim().ToLowerInvariant()}";
        if (!_rateLimiter.Acquire(partitionKey, _rateLimitOptions.Login).IsAllowed)
        {
            return Result.Failure<AuthenticationTokensResponse>(IdentityApplicationErrors.TooManyRequests);
        }

        Result<User> authenticated = await _authenticator
            .AuthenticateAsync(request.Email, request.Password, cancellationToken)
            .ConfigureAwait(false);

        if (authenticated.IsFailure)
        {
            return Result.Failure<AuthenticationTokensResponse>(authenticated.Error);
        }

        User user = authenticated.Value;

        Result<TenantMembership?> membershipResult = await ResolveMembershipAsync(
            user,
            request.TenantId,
            cancellationToken).ConfigureAwait(false);

        if (membershipResult.IsFailure)
        {
            return Result.Failure<AuthenticationTokensResponse>(membershipResult.Error);
        }

        return await _sessions
            .IssueAsync(user, membershipResult.Value, request.UserAgent, request.IpAddress, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Result<TenantMembership?>> ResolveMembershipAsync(
        User user,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId is { } requestedTenant)
        {
            if (requestedTenant == Guid.Empty)
            {
                return Result.Failure<TenantMembership?>(IdentityApplicationErrors.InvalidTenant);
            }

            TenantMembership? membership = await _memberships
                .FindAsync(TenantId.From(requestedTenant), user.Id, cancellationToken)
                .ConfigureAwait(false);

            if (membership is null || membership.Status != MembershipStatus.Active)
            {
                return Result.Failure<TenantMembership?>(IdentityApplicationErrors.InvalidTenant);
            }

            return Result.Success<TenantMembership?>(membership);
        }

        IReadOnlyCollection<TenantMembership> memberships = await _memberships
            .ListByUserAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        List<TenantMembership> active = memberships
            .Where(membership => membership.Status == MembershipStatus.Active)
            .ToList();

        if (active.Count == 0)
        {
            return Result.Success<TenantMembership?>(null);
        }

        if (active.Count == 1)
        {
            return Result.Success<TenantMembership?>((TenantMembership?)active[0]);
        }

        return Result.Failure<TenantMembership?>(IdentityApplicationErrors.InvalidTenant);
    }
}
