using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.Sessions;

/// <summary>Client metadata captured for a session (minimal; PII-minimized).</summary>
public sealed class SessionMetadata : ValueObject
{
    private SessionMetadata(string? userAgent, string? ipAddress)
    {
        UserAgent = userAgent;
        IpAddress = ipAddress;
    }

    /// <summary>Gets the user agent, if available.</summary>
    public string? UserAgent { get; }

    /// <summary>Gets the IP address, if available.</summary>
    public string? IpAddress { get; }

    /// <summary>Creates session metadata.</summary>
    /// <param name="userAgent">The user agent.</param>
    /// <param name="ipAddress">The IP address.</param>
    public static SessionMetadata Create(string? userAgent, string? ipAddress)
        => new(
            string.IsNullOrWhiteSpace(userAgent) ? null : Truncate(userAgent.Trim(), 512),
            string.IsNullOrWhiteSpace(ipAddress) ? null : Truncate(ipAddress.Trim(), 64));

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserAgent;
        yield return IpAddress;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
