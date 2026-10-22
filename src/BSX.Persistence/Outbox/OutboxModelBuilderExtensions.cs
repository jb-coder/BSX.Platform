using Microsoft.EntityFrameworkCore;

namespace BSX.Persistence.Outbox;

/// <summary>Applies the platform outbox mapping to a module model.</summary>
public static class OutboxModelBuilderExtensions
{
    /// <summary>Configures the <c>outbox_messages</c> table for the current module schema.</summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The same model builder for chaining.</returns>
    public static ModelBuilder ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        return modelBuilder;
    }
}
