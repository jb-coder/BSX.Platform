using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Memberships.ListTenantMemberships;

/// <summary>Handles <see cref="ListTenantMembershipsQuery"/>.</summary>
public sealed class ListTenantMembershipsQueryHandler
    : IQueryHandler<ListTenantMembershipsQuery, IReadOnlyList<MembershipDto>>
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ITenantContext _tenantContext;

    /// <summary>Initializes a new instance of the <see cref="ListTenantMembershipsQueryHandler"/> class.</summary>
    public ListTenantMembershipsQueryHandler(
        ITenantMembershipRepository memberships,
        IUserRepository users,
        IRoleRepository roles,
        ITenantContext tenantContext)
    {
        _memberships = memberships;
        _users = users;
        _roles = roles;
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MembershipDto>>> HandleAsync(
        ListTenantMembershipsQuery request,
        CancellationToken cancellationToken = default)
    {
        if (_tenantContext.TenantId is not { } tenantValue || tenantValue == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<MembershipDto>>(IdentityApplicationErrors.InvalidTenant);
        }

        TenantId tenantId = TenantId.From(tenantValue);
        IReadOnlyCollection<TenantMembership> memberships = await _memberships
            .ListByTenantAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        List<Role> roles = new();
        roles.AddRange(await _roles.ListByTenantAsync(tenantId, cancellationToken).ConfigureAwait(false));
        roles.AddRange(await _roles.ListByTenantAsync(null, cancellationToken).ConfigureAwait(false));
        Dictionary<Guid, string> roleNames = roles.ToDictionary(role => role.Id.Value, role => role.Name.Value);

        var result = new List<MembershipDto>(memberships.Count);

        foreach (TenantMembership membership in memberships)
        {
            User? user = await _users
                .GetByIdAsync(membership.UserId, cancellationToken)
                .ConfigureAwait(false);

            if (user is null)
            {
                continue;
            }

            Guid[] roleIds = membership.RoleIds.Select(roleId => roleId.Value).ToArray();

            result.Add(new MembershipDto(
                membership.Id.Value,
                membership.UserId.Value,
                user.Email.Value,
                user.Name.Value,
                membership.Status.ToString(),
                roleIds,
                roleIds
                    .Where(roleNames.ContainsKey)
                    .Select(roleId => roleNames[roleId])
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                membership.InvitedOnUtc,
                membership.JoinedOnUtc));
        }

        return Result.Success<IReadOnlyList<MembershipDto>>(
            result.OrderBy(membership => membership.Name, StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
