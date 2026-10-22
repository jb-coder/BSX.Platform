using BSX.Modules.Sales.Domain.DomainEvents;
using BSX.Modules.Sales.Domain.Enums;
using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Domain.Leads;

/// <summary>
/// An unqualified sales prospect. A lead advances from <see cref="LeadStatus.New"/> through
/// <see cref="LeadStatus.Contacted"/> to <see cref="LeadStatus.Qualified"/>, or is
/// <see cref="LeadStatus.Disqualified"/>. Qualified and disqualified leads are terminal: only their
/// notes remain editable.
/// </summary>
public sealed class Lead : AggregateRoot<LeadId>
{
    /// <summary>Maximum length of the free-text notes.</summary>
    public const int MaxNotesLength = 2000;

    private Lead(
        LeadId id,
        TenantId tenantId,
        LeadTitle title,
        CustomerId? customerId,
        OrganizationId? organizationId,
        string? notes)
        : base(id)
    {
        TenantId = tenantId;
        Title = title;
        CustomerId = customerId;
        OrganizationId = organizationId;
        Notes = notes;
        Status = LeadStatus.New;
    }

    private Lead()
    {
    }

    /// <summary>Gets the tenant identifier.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Gets the lead title.</summary>
    public LeadTitle Title { get; private set; } = null!;

    /// <summary>Gets the lifecycle status.</summary>
    public LeadStatus Status { get; private set; }

    /// <summary>Gets the optional CRM customer reference (identifier only).</summary>
    public CustomerId? CustomerId { get; private set; }

    /// <summary>Gets the optional CRM organization reference (identifier only).</summary>
    public OrganizationId? OrganizationId { get; private set; }

    /// <summary>Gets the free-text notes.</summary>
    public string? Notes { get; private set; }

    /// <summary>Gets a value indicating whether the lead details may still be edited.</summary>
    public bool IsEditable => Status is LeadStatus.New or LeadStatus.Contacted;

    /// <summary>Creates a new lead.</summary>
    /// <param name="id">The lead identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="title">The lead title.</param>
    /// <param name="customerId">The optional CRM customer reference.</param>
    /// <param name="organizationId">The optional CRM organization reference.</param>
    /// <param name="notes">The optional free-text notes.</param>
    public static Result<Lead> Create(
        LeadId id,
        TenantId tenantId,
        LeadTitle title,
        CustomerId? customerId = null,
        OrganizationId? organizationId = null,
        string? notes = null)
    {
        if (!TryNormalizeNotes(notes, out string? normalizedNotes))
        {
            return Result.Failure<Lead>(SalesErrors.LeadNotesInvalid);
        }

        var lead = new Lead(id, tenantId, title, customerId, organizationId, normalizedNotes);
        lead.RaiseDomainEvent(new LeadCreated(id, title));
        return Result.Success(lead);
    }

    /// <summary>Updates the lead details while the lead is still new or contacted.</summary>
    /// <param name="title">The new title.</param>
    /// <param name="customerId">The new optional CRM customer reference.</param>
    /// <param name="organizationId">The new optional CRM organization reference.</param>
    public Result UpdateDetails(
        LeadTitle title,
        CustomerId? customerId,
        OrganizationId? organizationId)
    {
        if (!IsEditable)
        {
            return Result.Failure(SalesErrors.LeadNotEditable);
        }

        Title = title;
        CustomerId = customerId;
        OrganizationId = organizationId;
        return Result.Success();
    }

    /// <summary>Marks a new lead as contacted.</summary>
    public Result MarkContacted()
    {
        if (Status == LeadStatus.Contacted)
        {
            return Result.Failure(SalesErrors.LeadAlreadyContacted);
        }

        if (Status != LeadStatus.New)
        {
            return Result.Failure(SalesErrors.LeadCannotBeContacted);
        }

        Status = LeadStatus.Contacted;
        return Result.Success();
    }

    /// <summary>Qualifies a contacted lead.</summary>
    public Result Qualify()
    {
        if (Status == LeadStatus.Qualified)
        {
            return Result.Failure(SalesErrors.LeadAlreadyQualified);
        }

        if (Status == LeadStatus.Disqualified)
        {
            return Result.Failure(SalesErrors.LeadCannotBeQualified);
        }

        if (Status != LeadStatus.Contacted)
        {
            return Result.Failure(SalesErrors.LeadMustBeContacted);
        }

        Status = LeadStatus.Qualified;
        RaiseDomainEvent(new LeadQualified(Id));
        return Result.Success();
    }

    /// <summary>Disqualifies a new or contacted lead.</summary>
    public Result Disqualify()
    {
        if (Status == LeadStatus.Disqualified)
        {
            return Result.Failure(SalesErrors.LeadAlreadyDisqualified);
        }

        if (Status == LeadStatus.Qualified)
        {
            return Result.Failure(SalesErrors.LeadCannotBeDisqualified);
        }

        Status = LeadStatus.Disqualified;
        RaiseDomainEvent(new LeadDisqualified(Id));
        return Result.Success();
    }

    /// <summary>Changes the free-text notes in any status, including qualified or disqualified.</summary>
    /// <param name="notes">The new notes, or <see langword="null"/> to clear them.</param>
    public Result ChangeNotes(string? notes)
    {
        if (!TryNormalizeNotes(notes, out string? normalizedNotes))
        {
            return Result.Failure(SalesErrors.LeadNotesInvalid);
        }

        Notes = normalizedNotes;
        return Result.Success();
    }

    private static bool TryNormalizeNotes(string? notes, out string? normalized)
    {
        normalized = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (normalized?.Length > MaxNotesLength)
        {
            normalized = null;
            return false;
        }

        return true;
    }
}
