namespace BSX.Modules.Crm.Contracts.Permissions;

/// <summary>Stable CRM permission codes published to the platform permission catalog (ADR-012).</summary>
public static class CrmPermissionCodes
{
    /// <summary>Read customers.</summary>
    public const string CustomersRead = "crm.customers.read";

    /// <summary>Create customers.</summary>
    public const string CustomersCreate = "crm.customers.create";

    /// <summary>Update customers and their contacts and addresses.</summary>
    public const string CustomersUpdate = "crm.customers.update";

    /// <summary>Read organizations.</summary>
    public const string OrganizationsRead = "crm.organizations.read";

    /// <summary>Create organizations.</summary>
    public const string OrganizationsCreate = "crm.organizations.create";

    /// <summary>Update organizations.</summary>
    public const string OrganizationsUpdate = "crm.organizations.update";
}
