using BSX.BuildingBlocks.Diagnostics;
using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BSX.Modules.Identity.Infrastructure.Permissions;

/// <summary>
/// Reconciles the platform system roles (<c>Administrator</c> and <c>Auditor</c>) with the
/// platform-wide permission catalog (ADR-018). Reconciliation is declarative: catalog permissions
/// are granted when missing and stale permissions are revoked, so a module removal cannot leave a
/// system role with capabilities that no longer exist.
/// </summary>
public sealed class SystemRolePermissionSynchronizer
{
    /// <summary>The system role that receives every catalog permission.</summary>
    public const string AdministratorRoleName = "Administrator";

    /// <summary>The system role that receives every catalog read permission.</summary>
    public const string AuditorRoleName = "Auditor";

    private readonly IdentityDbContext _context;
    private readonly PermissionCatalog _catalog;
    private readonly ILogger<SystemRolePermissionSynchronizer> _logger;

    /// <summary>Initializes a new instance of the <see cref="SystemRolePermissionSynchronizer"/> class.</summary>
    /// <param name="context">The Identity persistence context.</param>
    /// <param name="catalog">The platform-wide permission catalog.</param>
    /// <param name="logger">The logger.</param>
    public SystemRolePermissionSynchronizer(
        IdentityDbContext context,
        PermissionCatalog catalog,
        ILogger<SystemRolePermissionSynchronizer> logger)
    {
        _context = context;
        _catalog = catalog;
        _logger = logger;
    }

    /// <summary>Synchronizes the system roles with the platform permission catalog.</summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The reconciliation result.</returns>
    public async Task<PermissionSynchronizationResult> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        PlatformDiagnostics.PermissionSynchronizations.Add(1);

        try
        {
            _catalog.EnsureValid();
        }
        catch (InvalidOperationException exception)
        {
            PlatformDiagnostics.PermissionSynchronizationFailures.Add(1);
            _logger.LogCritical(exception, "The platform permission catalog is inconsistent.");
            throw;
        }

        PermissionDefinition[] catalog = _catalog.Permissions
            .OrderBy(permission => permission.Code, StringComparer.Ordinal)
            .ToArray();

        if (catalog.Length == 0)
        {
            _logger.LogDebug("No module permission catalogs are registered; system roles are unchanged.");
            return PermissionSynchronizationResult.Empty;
        }

        List<Role> systemRoles = await _context.Roles
            .Where(role => role.TenantId == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Role administrator = GetOrCreate(systemRoles, AdministratorRoleName);
        Role auditor = GetOrCreate(systemRoles, AuditorRoleName);

        int added = 0;
        var readCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (PermissionDefinition definition in catalog)
        {
            added += Grant(administrator, definition);

            if (IsReadPermission(definition.Code))
            {
                readCodes.Add(definition.Code);
                added += Grant(auditor, definition);
            }
        }

        var allCodes = new HashSet<string>(catalog.Select(permission => permission.Code), StringComparer.Ordinal);
        int removed = RevokeStale(administrator, allCodes) + RevokeStale(auditor, readCodes);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        PlatformDiagnostics.PermissionsAdded.Add(added);
        PlatformDiagnostics.PermissionsRemoved.Add(removed);

        _logger.LogInformation(
            "Reconciled {PermissionCount} catalog permission(s) into the system roles ({Added} added, {Removed} removed).",
            catalog.Length,
            added,
            removed);

        return new PermissionSynchronizationResult(catalog.Length, added, removed, systemRoles.Count);
    }

    private Role GetOrCreate(List<Role> systemRoles, string name)
    {
        Role? existing = systemRoles.FirstOrDefault(role => role.Name.Value == name);

        if (existing is not null)
        {
            return existing;
        }

        Role created = Role.Create(
            RoleId.New(),
            tenantId: null,
            RoleName.Create(name).Value,
            [],
            description: $"{name} system role.",
            isSystem: true).Value;

        // Startup synchronization is not a business occurrence; keep the aggregate free of events.
        created.ClearDomainEvents();
        _context.Roles.Add(created);
        systemRoles.Add(created);
        return created;
    }

    private static int Grant(Role role, PermissionDefinition definition)
    {
        if (role.Permissions.Any(permission => string.Equals(permission.Code, definition.Code, StringComparison.Ordinal)))
        {
            return 0;
        }

        Permission permission = Permission.Create(definition.Code, definition.Description).Value;
        return role.Grant(permission).IsSuccess ? 1 : 0;
    }

    private static int RevokeStale(Role role, HashSet<string> desiredCodes)
    {
        int removed = 0;

        foreach (Permission permission in role.Permissions
            .Where(permission => !desiredCodes.Contains(permission.Code))
            .ToArray())
        {
            if (role.Revoke(permission).IsSuccess)
            {
                removed++;
            }
        }

        return removed;
    }

    private static bool IsReadPermission(string code)
        => code.EndsWith(".read", StringComparison.Ordinal);
}

/// <summary>The outcome of a system role permission reconciliation.</summary>
/// <param name="CatalogCount">The number of permissions in the catalog.</param>
/// <param name="Added">The number of permissions granted.</param>
/// <param name="Removed">The number of stale permissions revoked.</param>
/// <param name="SystemRoleCount">The number of system roles reconciled.</param>
public sealed record PermissionSynchronizationResult(
    int CatalogCount,
    int Added,
    int Removed,
    int SystemRoleCount)
{
    /// <summary>An empty result used when no catalogs are registered.</summary>
    public static PermissionSynchronizationResult Empty { get; } = new(0, 0, 0, 0);
}
