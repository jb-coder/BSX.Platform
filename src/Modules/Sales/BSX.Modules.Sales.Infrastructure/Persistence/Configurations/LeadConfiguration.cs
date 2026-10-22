using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Sales.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="Lead"/> aggregate (ADR-004, ADR-010).</summary>
internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads", table =>
            table.HasCheckConstraint("CK_leads_tenant_key", "\"TenantKey\" = \"TenantId\""));
        builder.HasKey(lead => lead.Id);

        builder.Property(lead => lead.Id).HasConversion(SalesValueConverters.LeadId);
        builder.Property(lead => lead.TenantId).HasConversion(SalesValueConverters.TenantId).IsRequired();
        builder.Property(lead => lead.Title)
            .HasConversion(SalesValueConverters.LeadTitle)
            .HasMaxLength(LeadTitle.MaxLength)
            .IsRequired();
        builder.Property(lead => lead.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(lead => lead.CustomerId)
            .HasConversion(
                (CustomerId? value) => value == null ? null : value.Value,
                (Guid? value) => value == null ? null : CustomerId.From(value.Value));
        builder.Property(lead => lead.OrganizationId)
            .HasConversion(
                (OrganizationId? value) => value == null ? null : value.Value,
                (Guid? value) => value == null ? null : OrganizationId.From(value.Value));
        builder.Property(lead => lead.Notes).HasMaxLength(Lead.MaxNotesLength);

        builder.Ignore(lead => lead.DomainEvents);

        builder.ApplyAuditing();

        // Denormalized, lower-cased search column keeps searches provider-compatible.
        builder.Property<string>("SearchTitle").HasMaxLength(LeadTitle.MaxLength);

        // Primitive tenant column used by the global tenant query filter (provider-translatable).
        builder.Property<Guid>("TenantKey").IsRequired();
        builder.HasIndex("TenantKey");

        builder.HasIndex(lead => new { lead.TenantId, lead.CustomerId });
        builder.HasIndex("TenantId", "SearchTitle");
    }
}
