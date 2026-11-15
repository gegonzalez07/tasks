using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Tasks.Application.Common.Pagination;

/// <summary>
/// Generic pagination over <see cref="IQueryable{T}"/>. Works with EF Core (async, translated to SQL)
/// and with in-memory queryables (synchronous fallback), so it is unit-testable without a database.
/// </summary>
public static class QueryablePaginationExtensions
{
    /// <summary>
    /// Clamps the request to safe bounds: page ≥ 1 and 1 ≤ size ≤ <see cref="PagedRequest.MaxPageSize"/>.
    /// </summary>
    public static (int PageNumber, int PageSize) Normalize(this PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageSize = Math.Clamp(request.PageSize, 1, PagedRequest.MaxPageSize);
        var pageNumber = Math.Clamp(request.PageNumber, 1, int.MaxValue / PagedRequest.MaxPageSize);
        return (pageNumber, pageSize);
    }

    /// <summary>Applies Skip/Take for the requested page. Order the query first for deterministic pages.</summary>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> source, PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(source);
        var (pageNumber, pageSize) = request.Normalize();
        return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Counts the total and materialises the requested page.
    /// Order the query before calling this method so that paging is deterministic.
    /// </summary>
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> source,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var (pageNumber, pageSize) = request.Normalize();
        var normalized = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize };

        if (source.Provider is IAsyncQueryProvider)
        {
            var total = await source.CountAsync(cancellationToken).ConfigureAwait(false);
            if (total == 0)
            {
                return PagedResponse<T>.Empty(pageNumber, pageSize);
            }

            var items = await source.Paginate(normalized).ToListAsync(cancellationToken).ConfigureAwait(false);
            return new PagedResponse<T>(items, pageNumber, pageSize, total);
        }

        return source.ToPagedResponse(normalized);
    }

    /// <summary>Synchronous variant (LINQ-to-objects or providers without async support).</summary>
    public static PagedResponse<T> ToPagedResponse<T>(this IQueryable<T> source, PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(source);
        var (pageNumber, pageSize) = request.Normalize();
        var total = source.Count();
        if (total == 0)
        {
            return PagedResponse<T>.Empty(pageNumber, pageSize);
        }

        var items = source.Paginate(request).ToList();
        return new PagedResponse<T>(items, pageNumber, pageSize, total);
    }
}
