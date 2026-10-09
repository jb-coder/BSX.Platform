using BSX.Modules.Identity.Domain.ServiceAccounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.HasKey(apiKey => apiKey.Id);

        builder.Property(apiKey => apiKey.Id).HasConversion(IdentityValueConverters.ApiKeyId);
        builder.Property(apiKey => apiKey.PrincipalId).HasConversion(IdentityValueConverters.PrincipalId).IsRequired();
        builder.Property(apiKey => apiKey.PrincipalType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(apiKey => apiKey.Name).HasMaxLength(200).IsRequired();
        builder.Property(apiKey => apiKey.Prefix).HasConversion(IdentityValueConverters.ApiKeyPrefix).HasMaxLength(64).IsRequired();
        builder.Property(apiKey => apiKey.Hash).HasConversion(IdentityValueConverters.ApiKeyHash).HasMaxLength(512).IsRequired();
        builder.Property(apiKey => apiKey.CreatedOnUtc).IsRequired();
        builder.Property(apiKey => apiKey.ExpiresOnUtc);
        builder.Property(apiKey => apiKey.RevokedOnUtc);
        builder.Property(apiKey => apiKey.LastUsedOnUtc);

        builder.HasIndex(apiKey => apiKey.Prefix);

        builder.Ignore(apiKey => apiKey.DomainEvents);
        builder.Ignore(apiKey => apiKey.IsRevoked);

        builder.OwnsMany(apiKey => apiKey.Scopes, scopes =>
        {
            scopes.ToTable("api_key_scopes");
            scopes.WithOwner().HasForeignKey("ApiKeyId");
            scopes.Property(scope => scope.Code).HasColumnName("code").HasMaxLength(200);
            scopes.Property(scope => scope.Description).HasColumnName("description").HasMaxLength(500);
            scopes.HasKey("ApiKeyId", "Code");
        });

        builder.ApplyAuditing();
        builder.HasQueryFilter(apiKey => !EF.Property<bool>(apiKey, "IsDeleted"));
    }
}
