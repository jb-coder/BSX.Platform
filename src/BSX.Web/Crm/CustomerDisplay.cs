namespace BSX.Web.Crm;

/// <summary>Presentation helpers shared by customer screens.</summary>
public static class CustomerDisplay
{
    /// <summary>Returns the status badge CSS class.</summary>
    /// <param name="status">The customer status.</param>
    public static string StatusClass(string status)
        => string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)
            ? "status-badge status-active"
            : "status-badge status-inactive";

    /// <summary>Returns a compact organization label without exposing the full identifier.</summary>
    /// <param name="organizationId">The assigned organization, if any.</param>
    public static string OrganizationLabel(Guid? organizationId)
        => organizationId is null ? "Not assigned" : $"Assigned ({organizationId.Value.ToString("N")[..8]})";
}
