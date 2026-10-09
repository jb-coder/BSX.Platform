using BSX.Modules.Identity.Domain.ServiceAccounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class ServiceAccountConfiguration : IEntityTypeConfiguration<ServiceAccount>
{
    public void Configure(EntityTypeBuilder<ServiceAccount> builder)
    {
        builder.ToTable("service_accounts");
        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id).HasConversion(IdentityValueConverters.ServiceAccountId);
        builder.Property(account => account.TenantId).HasConversion(IdentityValueConverters.TenantId).IsRequired();
        builder.Property(account => account.Name).HasMaxLength(200).IsRequired();
        builder.Property(account => account.Enabled).IsRequired();

        builder.Ignore(account => account.DomainEvents);
        builder.Ignore(account => account.Principal);

        builder.OwnsMany(account => account.RoleIds, roles =>
        {
            roles.ToTable("service_account_roles");
            roles.WithOwner().HasForeignKey("ServiceAccountId");
            roles.Property(roleId => roleId.Value).HasColumnName("role_id");
            roles.HasKey("ServiceAccountId", "Value");
        });

        builder.ApplyAuditing();
        builder.HasQueryFilter(account => !EF.Property<bool>(account, "IsDeleted"));
    }
}
