namespace BSX.Contracts.Permissions;

/// <summary>
/// The platform-wide permission catalog. It aggregates every registered module catalog, removes
/// duplicate codes declared by more than one source and detects conflicting declarations
/// (ADR-012/ADR-018). The catalog is the single input to system role reconciliation.
/// </summary>
public sealed class PermissionCatalog
{
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

            if (!string.Equals(existing.Description, definition.Description, StringComparison.Ordinal))
            {
                conflicts.Add(new PermissionCatalogConflict(definition.Code, existing.Description, definition.Description));
            }
        }

        Permissions = permissions.Values.ToArray();
        Conflicts = conflicts;
    }

    /// <summary>Gets the deduplicated catalog permissions ordered by code.</summary>
    public IReadOnlyList<PermissionDefinition> Permissions { get; }

    /// <summary>Gets conflicting declarations (same code, different description).</summary>
    public IReadOnlyList<PermissionCatalogConflict> Conflicts { get; }

    /// <summary>Throws when two modules declare the same code with different descriptions.</summary>
    /// <exception cref="InvalidOperationException">When the catalog is inconsistent.</exception>
    public void EnsureValid()
    {
        if (Conflicts.Count == 0)
        {
            return;
        }

        string details = string.Join(
            "; ",
            Conflicts.Select(conflict => $"{conflict.Code} ('{conflict.FirstDescription}' vs '{conflict.SecondDescription}')"));

        throw new InvalidOperationException($"The platform permission catalog contains conflicting declarations: {details}");
    }
}

/// <summary>A conflicting permission declaration between two module catalogs.</summary>
/// <param name="Code">The duplicated permission code.</param>
/// <param name="FirstDescription">The first description encountered.</param>
/// <param name="SecondDescription">The conflicting description.</param>
public sealed record PermissionCatalogConflict(string Code, string FirstDescription, string SecondDescription);
