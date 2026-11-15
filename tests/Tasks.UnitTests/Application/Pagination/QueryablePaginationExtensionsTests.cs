using Tasks.Application.Common.Pagination;

namespace Tasks.UnitTests.Application.Pagination;

public sealed class QueryablePaginationExtensionsTests
{
    private static readonly IQueryable<int> TwentyFiveItems = Enumerable.Range(1, 25).AsQueryable();

    [Fact]
    public async Task ToPagedResponseAsync_MiddlePage_ReturnsSliceAndMetadata()
    {
        var result = await TwentyFiveItems.ToPagedResponseAsync(new PagedRequest { PageNumber = 2, PageSize = 10 });

        Assert.Equal(Enumerable.Range(11, 10), result.Items);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task ToPagedResponseAsync_LastPage_ReturnsRemainingItems()
    {
        var result = await TwentyFiveItems.ToPagedResponseAsync(new PagedRequest { PageNumber = 3, PageSize = 10 });

        Assert.Equal(new[] { 21, 22, 23, 24, 25 }, result.Items);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task ToPagedResponseAsync_PageBeyondRange_ReturnsNoItemsButKeepsTotal()
    {
        var result = await TwentyFiveItems.ToPagedResponseAsync(new PagedRequest { PageNumber = 10, PageSize = 10 });

        Assert.Empty(result.Items);
        Assert.Equal(25, result.TotalCount);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task ToPagedResponseAsync_EmptySource_ReturnsEmptyPage()
    {
        var result = await Enumerable.Empty<int>().AsQueryable().ToPagedResponseAsync(new PagedRequest());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Theory]
    [InlineData(0, 10, 1, 10)]
    [InlineData(-5, 10, 1, 10)]
    [InlineData(1, 0, 1, 1)]
    [InlineData(1, 500, 1, PagedRequest.MaxPageSize)]
    public void Normalize_ClampsOutOfRangeValues(int pageNumber, int pageSize, int expectedPage, int expectedSize)
    {
        var (page, size) = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize }.Normalize();

        Assert.Equal(expectedPage, page);
        Assert.Equal(expectedSize, size);
    }

    [Fact]
    public void Paginate_AppliesSkipAndTake()
    {
        var items = TwentyFiveItems.Paginate(new PagedRequest { PageNumber = 3, PageSize = 5 }).ToList();

        Assert.Equal(new[] { 11, 12, 13, 14, 15 }, items);
    }

    [Fact]
    public async Task ToPagedResponseAsync_NullSource_Throws()
    {
        IQueryable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>(() => source.ToPagedResponseAsync(new PagedRequest()));
    }
}
