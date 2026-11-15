namespace Tasks.Application.Common.Pagination;

/// <summary>
/// Standard pagination input (1-based page number). Inherit from it for paged queries with extra filters.
/// </summary>
public class PagedRequest
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    /// <summary>1-based page number.</summary>
    public int PageNumber { get; set; } = DefaultPageNumber;

    /// <summary>Items per page (1 - <see cref="MaxPageSize"/>).</summary>
    public int PageSize { get; set; } = DefaultPageSize;
}
