using BSX.BuildingBlocks.Events;
using BSX.SharedKernel.Primitives;

namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// Composes the module-owned units of work into the single platform <see cref="IUnitOfWork"/> used
/// by the command pipeline. It persists every module, commits their transactions, and dispatches
/// each domain event exactly once after commit (ADR-009).
/// </summary>
public sealed class CompositeUnitOfWork : IUnitOfWork
{
    private readonly IReadOnlyList<IModuleUnitOfWork> _modules;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private CompositeDatabaseTransaction? _transaction;

    /// <summary>Initializes a new instance of the <see cref="CompositeUnitOfWork"/> class.</summary>
    /// <param name="modules">The registered module units of work.</param>
    /// <param name="domainEventDispatcher">The post-commit domain event dispatcher.</param>
    /// <exception cref="InvalidOperationException">When no module unit of work is registered.</exception>
    public CompositeUnitOfWork(
        IEnumerable<IModuleUnitOfWork> modules,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);

        _modules = modules.ToArray();

        if (_modules.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one module unit of work must be registered before the composite unit of work is used.");
        }

        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <summary>Gets the names of the composed modules (diagnostics).</summary>
    public IReadOnlyList<string> ModuleNames => _modules.Select(module => module.ModuleName).ToArray();

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int written = 0;

        foreach (IModuleUnitOfWork module in _modules)
        {
            written += await module.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return written;
    }

    /// <inheritdoc />
    public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transactions = new List<IDatabaseTransaction>(_modules.Count);

        try
        {
            foreach (IModuleUnitOfWork module in _modules)
            {
                transactions.Add(await module.BeginTransactionAsync(cancellationToken).ConfigureAwait(false));
            }
        }
        catch
        {
            foreach (IDatabaseTransaction transaction in transactions)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }

        _transaction = new CompositeDatabaseTransaction(transactions);
        return _transaction;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAndDispatchAsync(CancellationToken cancellationToken = default)
    {
        IDomainEvent[] domainEvents = _modules
            .SelectMany(module => module.CollectPendingDomainEvents())
            .ToArray();

        int written = await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _transaction = null;
        }

        foreach (IModuleUnitOfWork module in _modules)
        {
            module.ClearPendingDomainEvents();
        }

        if (domainEvents.Length > 0)
        {
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
        }

        return written;
    }
}
