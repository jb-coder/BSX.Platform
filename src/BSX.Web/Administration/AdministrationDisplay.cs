using System.Globalization;

namespace BSX.Web.Administration;

/// <summary>Presentation helpers for administration screens.</summary>
public static class AdministrationDisplay
{
    /// <summary>Returns a badge class for a user status.</summary>
    public static string UserStatusClass(string status)
        => status switch
        {
            "Active" => "status-badge status-active",
            "Pending" => "status-badge status-lead-contacted",
            "Locked" => "status-badge status-lead-disqualified",
            "Disabled" => "status-badge status-inactive",
            _ => "status-badge status-inactive",
        };

    /// <summary>Returns a badge class for a membership status.</summary>
    public static string MembershipStatusClass(string status)
        => status switch
        {
            "Active" => "status-badge status-active",
            "Invited" => "status-badge status-lead-contacted",
            "Suspended" => "status-badge status-lead-disqualified",
            "Removed" => "status-badge status-inactive",
            _ => "status-badge status-inactive",
        };

    /// <summary>Returns a badge class for a session status.</summary>
    public static string SessionStatusClass(string status)
        => status switch
        {
            "Active" => "status-badge status-active",
            "Revoked" => "status-badge status-inactive",
            "Expired" => "status-badge status-lead-disqualified",
            _ => "status-badge status-inactive",
        };

    /// <summary>Formats a timestamp for administrative display.</summary>
    public static string Timestamp(DateTimeOffset value)
        => value.ToUniversalTime().ToString("dd MMM yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);

    /// <summary>Formats an optional timestamp.</summary>
    public static string Timestamp(DateTimeOffset? value)
        => value is null ? "—" : Timestamp(value.Value);

    /// <summary>Returns the module label for a permission code.</summary>
    public static string PermissionModule(string code)
    {
        int separator = code.IndexOf('.', StringComparison.Ordinal);
        string module = separator > 0 ? code[..separator] : code;
        return char.ToUpperInvariant(module[0]) + module[1..];
    }
}
