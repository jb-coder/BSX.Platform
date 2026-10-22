using BSX.Modules.Sales.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ValueObjects = BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Infrastructure.Persistence.Configurations;

/// <summary>Value converters for Sales identifiers and value objects.</summary>
internal static class SalesValueConverters
{
    public static readonly ValueConverter<LeadId, Guid> LeadId =
        new(value => value.Value, value => ValueObjects.LeadId.From(value));

    public static readonly ValueConverter<OpportunityId, Guid> OpportunityId =
        new(value => value.Value, value => ValueObjects.OpportunityId.From(value));

    public static readonly ValueConverter<CustomerId, Guid> CustomerId =
        new(value => value.Value, value => ValueObjects.CustomerId.From(value));

    public static readonly ValueConverter<OrganizationId, Guid> OrganizationId =
        new(value => value.Value, value => ValueObjects.OrganizationId.From(value));

    public static readonly ValueConverter<TenantId, Guid> TenantId =
        new(value => value.Value, value => ValueObjects.TenantId.From(value));

    public static readonly ValueConverter<LeadTitle, string> LeadTitle =
        new(value => value.Value, value => ValueObjects.LeadTitle.Create(value).Value);

    public static readonly ValueConverter<OpportunityTitle, string> OpportunityTitle =
        new(value => value.Value, value => ValueObjects.OpportunityTitle.Create(value).Value);

    public static readonly ValueConverter<Probability, int> Probability =
        new(value => value.Percent, value => ValueObjects.Probability.Create(value).Value);

    public static readonly ValueConverter<ExpectedCloseDate, DateOnly> ExpectedCloseDate =
        new(value => value.Value, value => ValueObjects.ExpectedCloseDate.Create(value, value).Value);
}
