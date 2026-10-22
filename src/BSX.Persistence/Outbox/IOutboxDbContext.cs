using Microsoft.EntityFrameworkCore;

namespace BSX.Persistence.Outbox;

/// <summary>
/// Implemented by every module <c>DbContext</c> that participates in the transactional outbox.
/// Each module owns its own outbox table/schema so that the row is written atomically with the
/// aggregate state (ADR-009).
/// </summary>
public interface IOutboxDbContext
{
    /// <summary>Gets the module's outbox messages.</summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>Gets the module name used for diagnostics and metrics.</summary>
    string ModuleName { get; }
}
