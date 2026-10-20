using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Crm.Infrastructure.Persistence.Configurations;

/// <summary>Adds the auditing, soft-delete and optimistic-concurrency shadow properties (ADR-010).</summary>
internal static class AuditingEntityTypeBuilderExtensions
{
    /// <summary>
    /// Declares the standard auditing shadow properties, skipping any that the entity already
    /// declares (for example a domain <c>TenantId</c>).
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="softDelete">Whether to add the soft-delete flag shadow property.</param>
    public static void ApplyAuditing(this EntityTypeBuilder builder, bool softDelete = true)
    {
        EnsureProperty<DateTimeOffset>(builder, "CreatedOnUtc", required: true);
        EnsureProperty<string>(builder, "CreatedBy");
        EnsureProperty<DateTimeOffset?>(builder, "UpdatedOnUtc");
        EnsureProperty<string>(builder, "UpdatedBy");
        EnsureProperty<DateTimeOffset?>(builder, "DeletedOnUtc");
        EnsureProperty<string>(builder, "DeletedBy");
        EnsureProperty<Guid?>(builder, "TenantId");

        if (softDelete)
        {
            EnsureProperty<bool>(builder, "IsDeleted", required: true, defaultValue: false);
        }

        if (builder.Metadata.FindProperty("Version") is null)
        {
            builder.Property<int>("Version").IsConcurrencyToken().IsRequired().HasDefaultValue(1);
        }
    }

    private static void EnsureProperty<TProperty>(
        EntityTypeBuilder builder,
        string propertyName,
        bool required = false,
        bool? defaultValue = null)
    {
        if (builder.Metadata.FindProperty(propertyName) is not null)
        {
            return;
        }

        PropertyBuilder<TProperty> property = builder.Property<TProperty>(propertyName);

        if (required)
        {
            property.IsRequired();
        }

        if (defaultValue.HasValue)
        {
            property.HasDefaultValue(defaultValue.Value);
        }
    }
}
