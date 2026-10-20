using BSX.SharedKernel.Primitives;

namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// A module-owned unit of work participating in the platform <see cref="CompositeUnitOfWork"/>
/// (ADR-009). Modules never register directly as <see cref="IUnitOfWork"/>.
/// </summary>
public interface IModuleUnitOfWork : IUnitOfWork
{
    /// <summary>Gets the module name (diagnostics).</summary>
    string ModuleName { get; }

    /// <summary>Collects the pending domain events without clearing them.</summary>
    IReadOnlyCollection<IDomainEvent> CollectPendingDomainEvents();

    /// <summary>Clears the pending domain events once they have been committed and dispatched.</summary>
    void ClearPendingDomainEvents();
}
