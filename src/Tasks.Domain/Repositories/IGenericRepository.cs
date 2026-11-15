using System.Linq.Expressions;
using Tasks.Domain.Common;

namespace Tasks.Domain.Repositories;

/// <summary>
/// Generic repository for <see cref="BaseEntity"/> types. It never persists by itself:
/// changes are committed through <see cref="IUnitOfWork"/>.
/// Soft-deleted entities are excluded from every query.
/// </summary>
public interface IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>Returns a tracked entity (so it can be updated) or <c>null</c>.</summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Composable query (e.g. for projections and pagination). Not tracked by default.
    /// </summary>
    IQueryable<TEntity> Query(bool asNoTracking = true);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    /// <summary>Marks the entity for (soft) deletion; applied on <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    void Remove(TEntity entity);
}
