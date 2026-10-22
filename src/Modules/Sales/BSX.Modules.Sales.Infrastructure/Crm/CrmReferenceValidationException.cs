namespace BSX.Modules.Sales.Infrastructure.Crm;

/// <summary>
/// Raised when a Sales aggregate references a CRM customer or organization that does not exist in
/// the active tenant. The API layer maps this to a controlled client error.
/// </summary>
public sealed class CrmReferenceValidationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="CrmReferenceValidationException"/> class.</summary>
    /// <param name="referenceType">The reference type (for example Customer or Organization).</param>
    /// <param name="referenceId">The invalid reference identifier.</param>
    /// <param name="tenantId">The tenant that owns the reference.</param>
    public CrmReferenceValidationException(string referenceType, Guid referenceId, Guid tenantId)
        : base($"The {referenceType} reference '{referenceId}' does not exist in tenant '{tenantId}'.")
    {
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        TenantId = tenantId;
    }

    /// <summary>Gets the invalid reference type.</summary>
    public string ReferenceType { get; }

    /// <summary>Gets the invalid reference identifier.</summary>
    public Guid ReferenceId { get; }

    /// <summary>Gets the tenant that owns the reference.</summary>
    public Guid TenantId { get; }
}
