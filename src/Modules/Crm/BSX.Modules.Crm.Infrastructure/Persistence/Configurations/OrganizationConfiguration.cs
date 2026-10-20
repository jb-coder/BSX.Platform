using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueObjects = BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="Organization"/> aggregate (ADR-004, ADR-010).</summary>
internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations", table =>
            table.HasCheckConstraint("CK_organizations_tenant_key", "\"TenantKey\" = \"TenantId\""));
        builder.HasKey(organization => organization.Id);

        builder.Property(organization => organization.Id).HasConversion(CrmValueConverters.OrganizationId);
        builder.Property(organization => organization.TenantId).HasConversion(CrmValueConverters.TenantId).IsRequired();
        builder.Property(organization => organization.Name)
            .HasConversion(CrmValueConverters.CustomerName)
            .HasMaxLength(CustomerName.MaxLength)
            .IsRequired();
        builder.Property(organization => organization.TaxIdentifier)
            .HasConversion(
                (TaxIdentifier? value) => value == null ? null : value.Value,
                (string? value) => value == null ? null : ValueObjects.TaxIdentifier.Create(value).Value)
            .HasMaxLength(TaxIdentifier.MaxLength);

        builder.Ignore(organization => organization.DomainEvents);

        builder.ApplyAuditing();

        builder.Property<string>("SearchName").HasMaxLength(CustomerName.MaxLength);
        builder.Property<string>("SearchTaxIdentifier").HasMaxLength(TaxIdentifier.MaxLength);

        // Primitive tenant column used by the global tenant query filter (provider-translatable).
        builder.Property<Guid>("TenantKey").IsRequired();
        builder.HasIndex("TenantKey");

        builder.HasIndex(organization => new { organization.TenantId, organization.Name });
        builder.HasIndex("TenantId", "SearchName");
        builder.HasIndex("TenantId", "SearchTaxIdentifier");

        builder.OwnsOne(organization => organization.Address, postal =>
        {
            postal.Property(value => value.Line1).HasColumnName("line1").HasMaxLength(Address.MaxLineLength).IsRequired();
            postal.Property(value => value.Line2).HasColumnName("line2").HasMaxLength(Address.MaxLineLength);
            postal.Property(value => value.City).HasColumnName("city").HasMaxLength(Address.MaxAreaLength).IsRequired();
            postal.Property(value => value.Region).HasColumnName("region").HasMaxLength(Address.MaxAreaLength);
            postal.Property(value => value.PostalCode).HasColumnName("postal_code").HasMaxLength(Address.MaxPostalCodeLength);
            postal.Property(value => value.Country).HasColumnName("country").HasMaxLength(Address.MaxCountryLength).IsRequired();
        });
    }
}
