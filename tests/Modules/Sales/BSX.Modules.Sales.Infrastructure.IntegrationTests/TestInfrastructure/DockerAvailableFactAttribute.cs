using System.Diagnostics;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;

/// <summary>
/// Marks a PostgreSQL parity test that requires a running Docker daemon. When Docker is not
/// available the test is skipped instead of failing; set <c>DOCKER_AVAILABLE=true</c> to require it.
/// </summary>
public sealed class DockerAvailableFactAttribute : FactAttribute
{
    private static readonly Lazy<bool> DockerAvailable = new(DetectDocker, isThreadSafe: true);

    public DockerAvailableFactAttribute()
    {
        if (!DockerAvailable.Value)
        {
            Skip = "Docker is not available. Start Docker (or set DOCKER_AVAILABLE=true to require it) "
                + "to run the PostgreSQL parity tests.";
        }
    }

    private static bool DetectDocker()
    {
        string? requested = Environment.GetEnvironmentVariable("DOCKER_AVAILABLE");

        if (!string.IsNullOrWhiteSpace(requested))
        {
            return string.Equals(requested, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(requested, "1", StringComparison.Ordinal);
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info --format {{.ServerVersion}}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(TimeSpan.FromSeconds(15)))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
