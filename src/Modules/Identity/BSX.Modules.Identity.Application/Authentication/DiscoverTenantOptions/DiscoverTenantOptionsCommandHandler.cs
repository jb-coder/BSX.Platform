using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Users;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication.DiscoverTenantOptions;

/// <summary>Handles <see cref="DiscoverTenantOptionsCommand"/>.</summary>
public sealed class DiscoverTenantOptionsCommandHandler
    : ICommandHandler<DiscoverTenantOptionsCommand, IReadOnlyList<TenantOptionDto>>
{
    private readonly CredentialAuthenticator _authenticator;
    private readonly ITenantMembershipRepository _memberships;
    private readonly IRateLimiter _rateLimiter;
    private readonly RateLimitOptions _rateLimitOptions;

    /// <summary>Initializes a new instance of the <see cref="DiscoverTenantOptionsCommandHandler"/> class.</summary>
    public DiscoverTenantOptionsCommandHandler(
        CredentialAuthenticator authenticator,
        ITenantMembershipRepository memberships,
        IRateLimiter rateLimiter,
        RateLimitOptions rateLimitOptions)
    {
        _authenticator = authenticator;
        _memberships = memberships;
        _rateLimiter = rateLimiter;
        _rateLimitOptions = rateLimitOptions;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TenantOptionDto>>> HandleAsync(
        DiscoverTenantOptionsCommand request,
        CancellationToken cancellationToken = default)
    {
        string partitionKey = $"tenant-discovery:{request.Email?.Trim().ToLowerInvariant()}";
        if (!_rateLimiter.Acquire(partitionKey, _rateLimitOptions.Login).IsAllowed)
        {
            return Result.Failure<IReadOnlyList<TenantOptionDto>>(IdentityApplicationErrors.TooManyRequests);
        }

        Result<User> authenticated = await _authenticator
            .AuthenticateAsync(request.Email, request.Password, cancellationToken)
            .ConfigureAwait(false);

        if (authenticated.IsFailure)
        {
            return Result.Failure<IReadOnlyList<TenantOptionDto>>(authenticated.Error);
        }

        IReadOnlyCollection<TenantMembership> memberships = await _memberships
            .ListByUserAsync(authenticated.Value.Id, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<TenantOptionDto> options = memberships
            .Where(membership => membership.Status == MembershipStatus.Active)
            .OrderBy(membership => membership.TenantId.Value)
            .Select(membership => new TenantOptionDto(membership.TenantId.Value, membership.JoinedOnUtc))
            .ToArray();

        return Result.Success(options);
    }
}
