using BSX.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace BSX.Modules.Identity.Infrastructure.Persistence;

/// <summary>Adapts an EF Core transaction to the platform <see cref="IDatabaseTransaction"/>.</summary>
internal sealed class DatabaseTransaction : IDatabaseTransaction
{
    private readonly IDbContextTransaction _transaction;

    /// <summary>Initializes a new instance of the <see cref="DatabaseTransaction"/> class.</summary>
    /// <param name="transaction">The EF Core transaction.</param>
    public DatabaseTransaction(IDbContextTransaction transaction) => _transaction = transaction;

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _transaction.CommitAsync(cancellationToken);

    /// <inheritdoc />
    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => _transaction.RollbackAsync(cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
