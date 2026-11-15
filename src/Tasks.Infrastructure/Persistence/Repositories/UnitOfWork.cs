using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tasks.Domain.Common;
using Tasks.Domain.Repositories;

namespace Tasks.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core unit of work: every repository it hands out shares the same <see cref="AppDbContext"/>,
/// so a single <see cref="SaveChangesAsync"/> (or transaction) commits all of them atomically.
/// </summary>
public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly Dictionary<Type, object> _repositories = [];
    private IDbContextTransaction? _currentTransaction;

    public bool HasActiveTransaction => _currentTransaction is not null;

    public IGenericRepository<TEntity> Repository<TEntity>()
        where TEntity : BaseEntity
    {
        if (_repositories.TryGetValue(typeof(TEntity), out var repository))
        {
            return (IGenericRepository<TEntity>)repository;
        }

        var created = new GenericRepository<TEntity>(_context);
        _repositories[typeof(TEntity)] = created;
        return created;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            throw new InvalidOperationException("There is no active transaction to commit.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _currentTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync().ConfigureAwait(false);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DisposeTransactionAsync().ConfigureAwait(false);
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Join the ambient transaction when one is already open (the outer scope commits).
        if (_currentTransaction is not null)
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }

        // The execution strategy may retry the whole block on transient failures (SQL Server retry policy).
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(
            async ct =>
            {
                await BeginTransactionAsync(ct).ConfigureAwait(false);
                try
                {
                    var result = await operation(ct).ConfigureAwait(false);
                    await CommitTransactionAsync(ct).ConfigureAwait(false);
                    return result;
                }
                catch
                {
                    await RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);

                    // Discard the failed changes so that a retry (or the caller) starts from a clean state.
                    _context.ChangeTracker.Clear();
                    throw;
                }
            },
            cancellationToken).ConfigureAwait(false);
    }

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return ExecuteInTransactionAsync<bool>(
            async ct =>
            {
                await operation(ct).ConfigureAwait(false);
                return true;
            },
            cancellationToken);
    }

    public async ValueTask DisposeAsync() => await DisposeTransactionAsync().ConfigureAwait(false);

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _currentTransaction = null;
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync().ConfigureAwait(false);
            _currentTransaction = null;
        }
    }
}
