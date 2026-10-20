using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueObjects = BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="Customer"/> aggregate (ADR-004, ADR-010).</summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id).HasConversion(CrmValueConverters.CustomerId);
        builder.Property(customer => customer.TenantId).HasConversion(CrmValueConverters.TenantId).IsRequired();
        builder.Property(customer => customer.Code)
            .HasConversion(CrmValueConverters.CustomerCode)
            .HasMaxLength(CustomerCode.MaxLength)
            .IsRequired();
        builder.Property(customer => customer.Name)
            .HasConversion(CrmValueConverters.CustomerName)
            .HasMaxLength(CustomerName.MaxLength)
            .IsRequired();
        builder.Property(customer => customer.TaxIdentifier)
            .HasConversion(
                (TaxIdentifier? value) => value == null ? null : value.Value,
                (string? value) => value == null ? null : ValueObjects.TaxIdentifier.Create(value).Value)
            .HasMaxLength(TaxIdentifier.MaxLength);
        builder.Property(customer => customer.OrganizationId)
            .HasConversion(
                (OrganizationId? value) => value == null ? (Guid?)null : value.Value,
                (Guid? value) => value == null ? (OrganizationId?)null : ValueObjects.OrganizationId.From(value.Value));
        builder.Property(customer => customer.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Ignore(customer => customer.DomainEvents);

        builder.ApplyAuditing();

        // Denormalized, lower-cased search columns keep searches provider-compatible
        // (SQLite tests and PostgreSQL production) and index-friendly.
        builder.Property<string>("SearchCode").HasMaxLength(CustomerCode.MaxLength);
        builder.Property<string>("SearchName").HasMaxLength(CustomerName.MaxLength);

        // Primitive tenant column used by the global tenant query filter (provider-translatable).
        builder.Property<Guid>("TenantKey").IsRequired();
        builder.HasIndex("TenantKey");

        builder.HasIndex(customer => new { customer.TenantId, customer.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(customer => new { customer.TenantId, customer.OrganizationId });
        builder.HasIndex("TenantId", "SearchCode");
        builder.HasIndex("TenantId", "SearchName");

        builder.OwnsMany(customer => customer.Addresses, addresses =>
        {
            addresses.ToTable("customer_addresses");
            addresses.WithOwner().HasForeignKey("CustomerId");
            addresses.HasKey(address => address.Id);
            addresses.Property(address => address.Id).HasConversion(CrmValueConverters.CustomerAddressId);
            addresses.Property(address => address.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            addresses.Property(address => address.IsDefault).IsRequired();

            addresses.OwnsOne(address => address.Address, postal =>
            {
                postal.Property(value => value.Line1).HasColumnName("line1").HasMaxLength(Address.MaxLineLength).IsRequired();
                postal.Property(value => value.Line2).HasColumnName("line2").HasMaxLength(Address.MaxLineLength);
                postal.Property(value => value.City).HasColumnName("city").HasMaxLength(Address.MaxAreaLength).IsRequired();
                postal.Property(value => value.Region).HasColumnName("region").HasMaxLength(Address.MaxAreaLength);
                postal.Property(value => value.PostalCode).HasColumnName("postal_code").HasMaxLength(Address.MaxPostalCodeLength);
                postal.Property(value => value.Country).HasColumnName("country").HasMaxLength(Address.MaxCountryLength).IsRequired();
            });
        });

        builder.OwnsMany(customer => customer.Contacts, contacts =>
        {
            contacts.ToTable("customer_contacts");
            contacts.WithOwner().HasForeignKey("CustomerId");
            contacts.HasKey(contact => contact.Id);
            contacts.Property(contact => contact.Id).HasConversion(CrmValueConverters.ContactPersonId);
            contacts.Property(contact => contact.Name)
                .HasConversion(CrmValueConverters.CustomerName)
                .HasMaxLength(CustomerName.MaxLength)
                .IsRequired();
            contacts.Property(contact => contact.Email)
                .HasConversion(
                    (Email? value) => value == null ? null : value.Value,
                    (string? value) => value == null ? null : ValueObjects.Email.Create(value).Value)
                .HasMaxLength(Email.MaxLength);
            contacts.Property(contact => contact.PhoneNumber)
                .HasConversion(
                    (PhoneNumber? value) => value == null ? null : value.Value,
                    (string? value) => value == null ? null : ValueObjects.PhoneNumber.Create(value).Value)
                .HasMaxLength(PhoneNumber.MaxLength);
            contacts.Property(contact => contact.IsPrimary).IsRequired();
        });
    }
}
