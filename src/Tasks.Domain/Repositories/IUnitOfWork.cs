using Tasks.Domain.Common;

namespace Tasks.Domain.Repositories;

/// <summary>
/// Coordinates the repositories that share the current scope and controls transactions.
/// </summary>
public interface IUnitOfWork
{
    bool HasActiveTransaction { get; }

    /// <summary>Returns the repository for <typeparamref name="TEntity"/> bound to this unit of work.</summary>
    IGenericRepository<TEntity> Repository<TEntity>()
        where TEntity : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an explicit transaction. Prefer <see cref="ExecuteInTransactionAsync{TResult}"/>,
    /// which is compatible with retrying execution strategies.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves pending changes and commits the active transaction (rolls back on failure).</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a transaction, saving and committing at the end.
    /// Any exception rolls the transaction back and is re-thrown.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
