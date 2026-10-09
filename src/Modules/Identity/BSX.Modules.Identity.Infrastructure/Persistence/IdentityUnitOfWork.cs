using BSX.BuildingBlocks.Events;
using BSX.BuildingBlocks.Persistence;
using BSX.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace BSX.Modules.Identity.Infrastructure.Persistence;

/// <summary>Identity unit of work implementing the ADR-009 persistence and dispatch seam.</summary>
internal sealed class IdentityUnitOfWork : UnitOfWorkBase
{
    private readonly IdentityDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IdentityUnitOfWork"/> class.</summary>
    /// <param name="context">The identity context.</param>
    /// <param name="domainEventDispatcher">The domain event dispatcher.</param>
    public IdentityUnitOfWork(IdentityDbContext context, IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher)
        => _context = context;

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public override async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        IDbContextTransaction transaction = await _context.Database
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
