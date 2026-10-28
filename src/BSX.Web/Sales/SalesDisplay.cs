using System.Globalization;

namespace BSX.Web.Sales;

/// <summary>Presentation helpers shared by Sales screens.</summary>
public static class SalesDisplay
{
    /// <summary>The ordered open sales stages.</summary>
    public static readonly IReadOnlyList<string> OpenStages =
    [
        "Discovery",
        "Qualification",
        "Proposal",
        "Negotiation",
    ];

    /// <summary>Formats a monetary amount with its currency.</summary>
    public static string Money(decimal amount, string currency)
        => $"{amount.ToString("N2", CultureInfo.InvariantCulture)} {currency}";

    /// <summary>Formats an expected close date.</summary>
    public static string Date(DateOnly date)
        => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>Returns the lifecycle rank for a lead status.</summary>
    public static int LeadStatusRank(string status)
        => status switch
        {
            "New" => 0,
            "Contacted" => 1,
            "Qualified" => 2,
            _ => -1,
        };

    /// <summary>Returns a status badge class for a lead status.</summary>
    public static string LeadStatusClass(string status)
        => status switch
        {
            "New" => "status-badge status-lead-new",
            "Contacted" => "status-badge status-lead-contacted",
            "Qualified" => "status-badge status-lead-qualified",
            "Disqualified" => "status-badge status-lead-disqualified",
            _ => "status-badge status-inactive",
        };

    /// <summary>Returns a stage badge class for an opportunity stage.</summary>
    public static string StageClass(string stage)
        => stage switch
        {
            "Discovery" => "status-badge status-stage-discovery",
            "Qualification" => "status-badge status-stage-qualification",
            "Proposal" => "status-badge status-stage-proposal",
            "Negotiation" => "status-badge status-stage-negotiation",
            "Won" => "status-badge status-stage-won",
            "Lost" => "status-badge status-stage-lost",
            _ => "status-badge status-inactive",
        };

    /// <summary>Gets the zero-based timeline index for a stage.</summary>
    public static int StageIndex(string stage)
        => stage switch
        {
            "Discovery" => 0,
            "Qualification" => 1,
            "Proposal" => 2,
            "Negotiation" => 3,
            "Won" => 4,
            "Lost" => 5,
            _ => -1,
        };

    /// <summary>Returns a compact CRM reference label.</summary>
    public static string Reference(Guid? id)
        => id is null ? "Not linked" : id.Value.ToString("N")[..8];
}
