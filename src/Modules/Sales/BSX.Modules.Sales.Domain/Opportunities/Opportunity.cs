using BSX.Modules.Sales.Domain.DomainEvents;
using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Domain.Opportunities;

/// <summary>
/// A qualified commercial opportunity created from a qualified lead. It moves through a fixed
/// sales pipeline and ends in the terminal <see cref="OpportunityStage.Won"/> or
/// <see cref="OpportunityStage.Lost"/> stage.
/// </summary>
public sealed class Opportunity : AggregateRoot<OpportunityId>
{
    private Opportunity(
        OpportunityId id,
        TenantId tenantId,
        LeadId leadId,
        OpportunityTitle title,
        CustomerId? customerId,
        OrganizationId? organizationId,
        Money amount,
        Probability probability,
        ExpectedCloseDate expectedCloseDate)
        : base(id)
    {
        TenantId = tenantId;
        LeadId = leadId;
        Title = title;
        CustomerId = customerId;
        OrganizationId = organizationId;
        Amount = amount;
        Probability = probability;
        ExpectedCloseDate = expectedCloseDate;
        Stage = OpportunityStage.Discovery;
    }

    private Opportunity()
    {
    }

    /// <summary>Gets the tenant identifier.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Gets the source lead identifier.</summary>
    public LeadId LeadId { get; private set; } = null!;

    /// <summary>Gets the opportunity title.</summary>
    public OpportunityTitle Title { get; private set; } = null!;

    /// <summary>Gets the optional CRM customer reference (identifier only).</summary>
    public CustomerId? CustomerId { get; private set; }

    /// <summary>Gets the optional CRM organization reference (identifier only).</summary>
    public OrganizationId? OrganizationId { get; private set; }

    /// <summary>Gets the potential revenue amount.</summary>
    public Money Amount { get; private set; } = null!;

    /// <summary>Gets the probability of winning.</summary>
    public Probability Probability { get; private set; } = null!;

    /// <summary>Gets the expected close date.</summary>
    public ExpectedCloseDate ExpectedCloseDate { get; private set; } = null!;

    /// <summary>Gets the current sales stage.</summary>
    public OpportunityStage Stage { get; private set; }

    /// <summary>Gets a value indicating whether the opportunity is won or lost.</summary>
    public bool IsTerminal => Stage is OpportunityStage.Won or OpportunityStage.Lost;

    /// <summary>Gets the revenue forecast: the amount weighted by the probability of winning.</summary>
    public Money WeightedAmount => Amount.ApplyPercentage(Probability.Percent);

    /// <summary>Creates an opportunity from a qualified lead.</summary>
    /// <param name="id">The opportunity identifier.</param>
    /// <param name="tenantId">The tenant identifier; must match the lead's tenant.</param>
    /// <param name="qualifiedLead">The qualified source lead.</param>
    /// <param name="title">The opportunity title.</param>
    /// <param name="amount">The potential revenue amount.</param>
    /// <param name="probability">The probability of winning.</param>
    /// <param name="expectedCloseDate">The expected close date.</param>
    /// <exception cref="ArgumentNullException">Thrown when the lead is null.</exception>
    public static Result<Opportunity> CreateFromLead(
        OpportunityId id,
        TenantId tenantId,
        Lead qualifiedLead,
        OpportunityTitle title,
        Money amount,
        Probability probability,
        ExpectedCloseDate expectedCloseDate)
    {
        ArgumentNullException.ThrowIfNull(qualifiedLead);

        if (qualifiedLead.TenantId != tenantId)
        {
            return Result.Failure<Opportunity>(SalesErrors.OpportunityTenantMismatch);
        }

        if (qualifiedLead.Status != LeadStatus.Qualified)
        {
            return Result.Failure<Opportunity>(SalesErrors.OpportunityLeadNotQualified);
        }

        var opportunity = new Opportunity(
            id,
            tenantId,
            qualifiedLead.Id,
            title,
            qualifiedLead.CustomerId,
            qualifiedLead.OrganizationId,
            amount,
            probability,
            expectedCloseDate);

        opportunity.RaiseDomainEvent(new OpportunityCreated(id, qualifiedLead.Id, title));
        return Result.Success(opportunity);
    }

    /// <summary>Updates the title and CRM references while the opportunity is open.</summary>
    /// <param name="title">The new title.</param>
    /// <param name="customerId">The new optional CRM customer reference.</param>
    /// <param name="organizationId">The new optional CRM organization reference.</param>
    public Result UpdateDetails(
        OpportunityTitle title,
        CustomerId? customerId,
        OrganizationId? organizationId)
    {
        if (IsTerminal)
        {
            return Result.Failure(SalesErrors.OpportunityTerminal);
        }

        Title = title;
        CustomerId = customerId;
        OrganizationId = organizationId;
        return Result.Success();
    }

    /// <summary>Updates the revenue forecast while the opportunity is open.</summary>
    /// <param name="amount">The new potential revenue amount.</param>
    /// <param name="probability">The new probability of winning.</param>
    /// <param name="expectedCloseDate">The new expected close date.</param>
    public Result UpdateForecast(
        Money amount,
        Probability probability,
        ExpectedCloseDate expectedCloseDate)
    {
        if (IsTerminal)
        {
            return Result.Failure(SalesErrors.OpportunityTerminal);
        }

        Amount = amount;
        Probability = probability;
        ExpectedCloseDate = expectedCloseDate;
        return Result.Success();
    }

    /// <summary>Advances the opportunity to the next sales stage.</summary>
    public Result AdvanceStage()
    {
        if (IsTerminal)
        {
            return Result.Failure(SalesErrors.OpportunityTerminal);
        }

        OpportunityStage next = Stage switch
        {
            OpportunityStage.Discovery => OpportunityStage.Qualification,
            OpportunityStage.Qualification => OpportunityStage.Proposal,
            OpportunityStage.Proposal => OpportunityStage.Negotiation,
            _ => Stage,
        };

        if (next == Stage)
        {
            return Result.Failure(SalesErrors.OpportunityCannotAdvance);
        }

        OpportunityStage previous = Stage;
        Stage = next;
        RaiseDomainEvent(new OpportunityStageChanged(Id, previous, next));
        return Result.Success();
    }

    /// <summary>Marks a negotiation as won. Won and lost are terminal.</summary>
    public Result MarkWon()
    {
        if (IsTerminal)
        {
            return Result.Failure(SalesErrors.OpportunityTerminal);
        }

        if (Stage != OpportunityStage.Negotiation)
        {
            return Result.Failure(SalesErrors.OpportunityInvalidStageTransition);
        }

        Stage = OpportunityStage.Won;
        Probability = Probability.Certain;
        RaiseDomainEvent(new OpportunityWon(Id));
        return Result.Success();
    }

    /// <summary>Marks an open opportunity as lost. Won and lost are terminal.</summary>
    public Result MarkLost()
    {
        if (IsTerminal)
        {
            return Result.Failure(SalesErrors.OpportunityTerminal);
        }

        Stage = OpportunityStage.Lost;
        Probability = Probability.None;
        RaiseDomainEvent(new OpportunityLost(Id));
        return Result.Success();
    }
}
