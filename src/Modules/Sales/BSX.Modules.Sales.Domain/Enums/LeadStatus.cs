namespace BSX.Modules.Sales.Domain.Enums;

/// <summary>The lifecycle status of a lead.</summary>
public enum LeadStatus
{
    /// <summary>The lead has been created but not contacted.</summary>
    New = 0,

    /// <summary>The lead has been contacted.</summary>
    Contacted = 1,

    /// <summary>The lead has been qualified and can become an opportunity.</summary>
    Qualified = 2,

    /// <summary>The lead has been disqualified.</summary>
    Disqualified = 3,
}
