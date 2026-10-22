namespace BSX.Modules.Crm.Application.Common;

/// <summary>
/// The application-facing address purpose. It intentionally mirrors the domain enum so transport
/// adapters never reference the CRM domain assembly (ADR-001/ADR-015).
/// </summary>
public enum CrmAddressType
{
    /// <summary>A billing address.</summary>
    Billing = 0,

    /// <summary>A shipping address.</summary>
    Shipping = 1,

    /// <summary>Any other address.</summary>
    Other = 2,
}
