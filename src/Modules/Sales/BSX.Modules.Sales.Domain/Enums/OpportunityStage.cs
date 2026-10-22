namespace BSX.Modules.Sales.Domain.Enums;

/// <summary>The sales stage of an opportunity.</summary>
public enum OpportunityStage
{
    /// <summary>The opportunity is being discovered.</summary>
    Discovery = 0,

    /// <summary>The opportunity is being qualified.</summary>
    Qualification = 1,

    /// <summary>A proposal has been made.</summary>
    Proposal = 2,

    /// <summary>The opportunity is under negotiation.</summary>
    Negotiation = 3,

    /// <summary>The opportunity has been won (terminal).</summary>
    Won = 4,

    /// <summary>The opportunity has been lost (terminal).</summary>
    Lost = 5,
}
