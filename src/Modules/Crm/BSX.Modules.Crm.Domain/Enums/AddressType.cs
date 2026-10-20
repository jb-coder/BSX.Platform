namespace BSX.Modules.Crm.Domain.Enums;

/// <summary>The purpose of a customer address.</summary>
public enum AddressType
{
    /// <summary>A billing address.</summary>
    Billing = 0,

    /// <summary>A shipping address.</summary>
    Shipping = 1,

    /// <summary>Any other address.</summary>
    Other = 2,
}
