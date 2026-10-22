namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// Resolves the database connection used by the EF Core design-time factories. Deployment tooling
/// passes <c>--connection "..."</c> after <c>--</c>; the module environment variable is the fallback
/// so migration bundles/agents can supply it without committing credentials.
/// </summary>
public static class DesignTimeConnection
{
    /// <summary>Resolves the connection string for a module.</summary>
    /// <param name="args">The arguments passed by the EF Core tools.</param>
    /// <param name="moduleName">The module name used for the environment variable fallback.</param>
    /// <param name="fallback">The local development connection string.</param>
    /// <returns>The resolved connection string.</returns>
    public static string Resolve(string[] args, string moduleName, string fallback)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);

        string? argument = FindValue(args, "--connection") ?? FindValue(args, "--connection-string");
        if (!string.IsNullOrWhiteSpace(argument))
        {
            return argument;
        }

        string? environment = Environment.GetEnvironmentVariable($"ConnectionStrings__{moduleName}");
        return string.IsNullOrWhiteSpace(environment) ? fallback : environment;
    }

    private static string? FindValue(string[] args, string name)
    {
        for (int index = 0; index < args.Length - 1; index++)
        {
            if (!string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = args[index + 1];
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}
