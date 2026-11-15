namespace Tasks.Application.Common.Pagination;

/// <summary>
/// Standard paged result: the items of the requested page plus navigation metadata.
/// </summary>
public sealed record PagedResponse<T>
{
    public PagedResponse(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResponse<T> Empty(int pageNumber, int pageSize) => new([], pageNumber, pageSize, 0);

    /// <summary>Projects the items keeping the pagination metadata.</summary>
    public PagedResponse<TOut> Map<TOut>(Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new PagedResponse<TOut>(Items.Select(selector).ToList(), PageNumber, PageSize, TotalCount);
    }
}
