namespace BSX.Contracts.Permissions;

/// <summary>
/// The platform-wide permission catalog. It aggregates every registered module catalog, removes
/// duplicate codes declared by more than one source and detects conflicting declarations
/// (ADR-012/ADR-018). The catalog is the single input to system role reconciliation and the
/// authoritative source of permission scope (ADR-020).
/// </summary>
public sealed class PermissionCatalog
{
    private readonly Dictionary<string, PermissionDefinition> _byCode;

    /// <summary>Initializes a new instance of the <see cref="PermissionCatalog"/> class.</summary>
    /// <param name="sources">The module permission catalog sources.</param>
    public PermissionCatalog(IEnumerable<IPermissionCatalogSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var permissions = new Dictionary<string, PermissionDefinition>(StringComparer.Ordinal);
        var conflicts = new List<PermissionCatalogConflict>();

        foreach (PermissionDefinition definition in sources
            .Where(source => source is not null)
            .SelectMany(source => source.Permissions)
            .OrderBy(definition => definition.Code, StringComparer.Ordinal))
        {
            if (!permissions.TryGetValue(definition.Code, out PermissionDefinition? existing))
            {
                permissions.Add(definition.Code, definition);
                continue;
            }

            if (existing.Scope != definition.Scope
                || !string.Equals(existing.Description, definition.Description, StringComparison.Ordinal))
            {
                conflicts.Add(new PermissionCatalogConflict(
                    definition.Code,
                    existing.Description,
                    existing.Scope,
                    definition.Description,
                    definition.Scope));
            }
        }

        _byCode = permissions;
        Permissions = permissions.Values.ToArray();
        Conflicts = conflicts;
    }

    /// <summary>Gets the deduplicated catalog permissions ordered by code.</summary>
    public IReadOnlyList<PermissionDefinition> Permissions { get; }

    /// <summary>Gets conflicting declarations (same code, different description or scope).</summary>
    public IReadOnlyList<PermissionCatalogConflict> Conflicts { get; }

    /// <summary>Determines whether the catalog declares the given permission code.</summary>
    /// <param name="code">The permission code.</param>
    public bool Contains(string? code)
        => !string.IsNullOrWhiteSpace(code) && _byCode.ContainsKey(code);

    /// <summary>Tries to resolve a permission definition by code.</summary>
    /// <param name="code">The permission code.</param>
    /// <param name="definition">The matching definition, when found.</param>
    public bool TryGet(string? code, out PermissionDefinition? definition)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            definition = null;
            return false;
        }

        return _byCode.TryGetValue(code, out definition);
    }

    /// <summary>Returns the scope of a permission code, or <see langword="null"/> when unknown.</summary>
    /// <param name="code">The permission code.</param>
    public PermissionScope? ScopeOf(string? code)
        => TryGet(code, out PermissionDefinition? definition) ? definition!.Scope : null;

    /// <summary>Returns the catalog permissions declared for a scope, ordered by code.</summary>
    /// <param name="scope">The permission scope.</param>
    public IReadOnlyList<PermissionDefinition> ForScope(PermissionScope scope)
        => Permissions.Where(permission => permission.Scope == scope).ToArray();

    /// <summary>Throws when two modules declare the same code with different descriptions or scopes.</summary>
    /// <exception cref="InvalidOperationException">When the catalog is inconsistent.</exception>
    public void EnsureValid()
    {
        if (Conflicts.Count == 0)
        {
            return;
        }

        string details = string.Join(
            "; ",
            Conflicts.Select(conflict =>
                $"{conflict.Code} ('{conflict.FirstDescription}' [{conflict.FirstScope}] vs '{conflict.SecondDescription}' [{conflict.SecondScope}])"));

        throw new InvalidOperationException($"The platform permission catalog contains conflicting declarations: {details}");
    }
}

/// <summary>A conflicting permission declaration between two module catalogs.</summary>
/// <param name="Code">The duplicated permission code.</param>
/// <param name="FirstDescription">The first description encountered.</param>
/// <param name="FirstScope">The scope of the first declaration.</param>
/// <param name="SecondDescription">The conflicting description.</param>
/// <param name="SecondScope">The scope of the conflicting declaration.</param>
public sealed record PermissionCatalogConflict(
    string Code,
    string FirstDescription,
    PermissionScope FirstScope,
    string SecondDescription,
    PermissionScope SecondScope);
