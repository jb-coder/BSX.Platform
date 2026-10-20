using BSX.BuildingBlocks.Events;
using BSX.BuildingBlocks.Persistence;
using BSX.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BSX.Modules.Crm.Infrastructure.Persistence;

/// <summary>CRM unit of work implementing the ADR-009 persistence and dispatch seam.</summary>
internal sealed class CrmUnitOfWork : UnitOfWorkBase, IModuleUnitOfWork
{
    private readonly CrmDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="CrmUnitOfWork"/> class.</summary>
    /// <param name="context">The CRM context.</param>
    /// <param name="domainEventDispatcher">The domain event dispatcher.</param>
    public CrmUnitOfWork(CrmDbContext context, IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher)
        => _context = context;

    /// <inheritdoc />
    public string ModuleName => "Crm";

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> CollectPendingDomainEvents() => CollectDomainEvents();

    /// <inheritdoc />
    public void ClearPendingDomainEvents() => ClearDomainEvents();

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("A concurrent modification was detected.", exception);
        }
    }

    /// <inheritdoc />
    public override async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        return new DatabaseTransaction(transaction);
    }

    /// <inheritdoc />
    protected override IReadOnlyCollection<IDomainEvent> CollectDomainEvents()
        => _context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToArray();

    /// <inheritdoc />
    protected override void ClearDomainEvents()
    {
        foreach (EntityEntry<IHasDomainEvents> entry in _context.ChangeTracker.Entries<IHasDomainEvents>())
        {
            entry.Entity.ClearDomainEvents();
        }
    }

    /// <inheritdoc />
    protected override async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            await _context.Database.CurrentTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
