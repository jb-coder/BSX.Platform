using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Sales.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="Opportunity"/> aggregate (ADR-004, ADR-010).</summary>
internal sealed class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        builder.ToTable("opportunities", table =>
            table.HasCheckConstraint("CK_opportunities_tenant_key", "\"TenantKey\" = \"TenantId\""));
        builder.HasKey(opportunity => opportunity.Id);

        builder.Property(opportunity => opportunity.Id).HasConversion(SalesValueConverters.OpportunityId);
        builder.Property(opportunity => opportunity.TenantId)
            .HasConversion(SalesValueConverters.TenantId)
            .IsRequired();
        builder.Property(opportunity => opportunity.LeadId)
            .HasConversion(SalesValueConverters.LeadId)
            .IsRequired();
        builder.Property(opportunity => opportunity.CustomerId)
            .HasConversion(SalesValueConverters.CustomerId)
            .IsRequired();
        builder.Property(opportunity => opportunity.OrganizationId)
            .HasConversion(
                (OrganizationId? value) => value == null ? null : value.Value,
                (Guid? value) => value == null ? null : OrganizationId.From(value.Value));
        builder.Property(opportunity => opportunity.Title)
            .HasConversion(SalesValueConverters.OpportunityTitle)
            .HasMaxLength(OpportunityTitle.MaxLength)
            .IsRequired();
        builder.Property(opportunity => opportunity.Probability)
            .HasConversion(SalesValueConverters.Probability)
            .IsRequired();
        builder.Property(opportunity => opportunity.ExpectedCloseDate)
            .HasConversion(SalesValueConverters.ExpectedCloseDate)
            .IsRequired();
        builder.Property(opportunity => opportunity.Stage).HasConversion<string>().HasMaxLength(32).IsRequired();

        // Money is a value object stored in the opportunity row. A complex type is tracked by
        // value, so replacing the instance during an update persists the new amount and currency.
        builder.ComplexProperty(opportunity => opportunity.Amount, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();
            money.Property(value => value.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Ignore(opportunity => opportunity.DomainEvents);

        builder.ApplyAuditing();

        builder.Property<string>("SearchTitle").HasMaxLength(OpportunityTitle.MaxLength);

        // Primitive tenant column used by the global tenant query filter (provider-translatable).
        builder.Property<Guid>("TenantKey").IsRequired();
        builder.HasIndex("TenantKey");

        // One qualified lead may produce only one (non-deleted) opportunity per tenant.
        builder.HasIndex(opportunity => new { opportunity.TenantId, opportunity.LeadId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(opportunity => new { opportunity.TenantId, opportunity.CustomerId });
        builder.HasIndex("TenantId", "SearchTitle");
    }
}
