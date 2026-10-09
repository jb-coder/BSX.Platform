using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Errors;
using BSX.Modules.Identity.Domain.Primitives;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.ServiceAccounts;

/// <summary>
/// A machine principal that authenticates with API keys and is authorized like a user
/// (ADR-014).
/// </summary>
public sealed class ServiceAccount : AggregateRoot<ServiceAccountId>, IPrincipal
{
    private readonly HashSet<RoleId> _roleIds = [];

    private ServiceAccount(ServiceAccountId id, TenantId tenantId, string name)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Enabled = true;
    }

    private ServiceAccount()
    {
    }

    /// <summary>Gets the owning tenant.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Gets the service account name.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Gets a value indicating whether the account is enabled.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Gets the assigned role identifiers.</summary>
    public IReadOnlyCollection<RoleId> RoleIds => _roleIds.ToArray();

    /// <inheritdoc />
    public PrincipalId Principal => PrincipalId.From(Id.Value);

    /// <summary>Creates a service account.</summary>
    /// <param name="id">The service account identifier.</param>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="name">The service account name.</param>
    public static Result<ServiceAccount> Create(ServiceAccountId id, TenantId tenantId, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ServiceAccount>(IdentityErrors.NameInvalid);
        }

        var account = new ServiceAccount(id, tenantId, name.Trim());
        account.RaiseDomainEvent(new ServiceAccountCreated(id, tenantId, account.Name));
        return Result.Success(account);
    }

    /// <summary>Enables the service account.</summary>
    public Result Enable()
    {
        if (Enabled)
        {
            return Result.Failure(IdentityErrors.ServiceAccountAlreadyEnabled);
        }

        Enabled = true;
        RaiseDomainEvent(new ServiceAccountEnabled(Id));
        return Result.Success();
    }

    /// <summary>Disables the service account.</summary>
    public Result Disable()
    {
        if (!Enabled)
        {
            return Result.Failure(IdentityErrors.ServiceAccountAlreadyDisabled);
        }

        Enabled = false;
        RaiseDomainEvent(new ServiceAccountDisabled(Id));
        return Result.Success();
    }

    /// <summary>Assigns a role to the service account.</summary>
    /// <param name="roleId">The role identifier.</param>
    public Result AssignRole(RoleId roleId)
    {
        if (!_roleIds.Add(roleId))
        {
            return Result.Failure(IdentityErrors.ServiceAccountRoleAlreadyAssigned);
        }

        RaiseDomainEvent(new ServiceAccountRoleAssigned(Id, roleId));
        return Result.Success();
    }

    /// <summary>Removes a role from the service account.</summary>
    /// <param name="roleId">The role identifier.</param>
    public Result RemoveRole(RoleId roleId)
    {
        if (!_roleIds.Remove(roleId))
        {
            return Result.Failure(IdentityErrors.ServiceAccountRoleNotAssigned);
        }

        RaiseDomainEvent(new ServiceAccountRoleRemoved(Id, roleId));
        return Result.Success();
    }
}
