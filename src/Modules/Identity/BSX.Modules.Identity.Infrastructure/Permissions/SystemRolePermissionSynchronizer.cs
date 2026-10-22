using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BSX.Modules.Identity.Infrastructure.Permissions;

/// <summary>
/// Idempotently reconciles the platform system roles (<c>Administrator</c> and <c>Auditor</c>) with
/// the permissions declared by the registered module catalogs (ADR-018). Reconciliation is
/// additive: catalog permissions are granted when missing, and existing grants are never removed,
/// so a partial deployment cannot strip a role's capabilities.
/// </summary>
public sealed class SystemRolePermissionSynchronizer
{
    /// <summary>The system role that receives every catalog permission.</summary>
    public const string AdministratorRoleName = "Administrator";

    /// <summary>The system role that receives every catalog read permission.</summary>
    public const string AuditorRoleName = "Auditor";

    private readonly IdentityDbContext _context;
    private readonly IEnumerable<IPermissionCatalogSource> _catalogSources;
    private readonly ILogger<SystemRolePermissionSynchronizer> _logger;

    /// <summary>Initializes a new instance of the <see cref="SystemRolePermissionSynchronizer"/> class.</summary>
    /// <param name="context">The Identity persistence context.</param>
    /// <param name="catalogSources">The permission catalogs published by the composed modules.</param>
    /// <param name="logger">The logger.</param>
    public SystemRolePermissionSynchronizer(
        IdentityDbContext context,
        IEnumerable<IPermissionCatalogSource> catalogSources,
        ILogger<SystemRolePermissionSynchronizer> logger)
    {
        _context = context;
        _catalogSources = catalogSources;
        _logger = logger;
    }

    /// <summary>Synchronizes the system roles with the registered permission catalogs.</summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        PermissionDefinition[] catalog = _catalogSources
            .SelectMany(source => source.Permissions)
            .GroupBy(permission => permission.Code, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(permission => permission.Code, StringComparer.Ordinal)
            .ToArray();

        if (catalog.Length == 0)
        {
            _logger.LogDebug("No module permission catalogs are registered; system roles are unchanged.");
            return;
        }

        List<Role> systemRoles = await _context.Roles
            .Where(role => role.TenantId == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Role administrator = GetOrCreate(systemRoles, AdministratorRoleName);
        Role auditor = GetOrCreate(systemRoles, AuditorRoleName);

        foreach (PermissionDefinition definition in catalog)
        {
            Grant(administrator, definition);

            if (IsReadPermission(definition.Code))
            {
                Grant(auditor, definition);
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Synchronized {PermissionCount} catalog permission(s) into the system roles.",
            catalog.Length);
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

    private static void Grant(Role role, PermissionDefinition definition)
    {
        if (role.Permissions.Any(permission => string.Equals(permission.Code, definition.Code, StringComparison.Ordinal)))
        {
            return;
        }

        Permission permission = Permission.Create(definition.Code, definition.Description).Value;
        role.Grant(permission);
    }

    private static bool IsReadPermission(string code)
        => code.EndsWith(".read", StringComparison.Ordinal);
}
