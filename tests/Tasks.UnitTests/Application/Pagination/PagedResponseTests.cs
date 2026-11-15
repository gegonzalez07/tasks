using AutoFixture;
using Tasks.Application.Common.Pagination;

namespace Tasks.UnitTests.Application.Pagination;

public sealed class PagedResponseTests
{
    private readonly Fixture _fixture = new();

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(101, 25, 5)]
    public void TotalPages_IsCeilingOfTotalOverPageSize(int totalCount, int pageSize, int expected)
    {
        var response = new PagedResponse<string>([], 1, pageSize, totalCount);

        Assert.Equal(expected, response.TotalPages);
    }

    [Fact]
    public void Map_ProjectsItemsAndKeepsMetadata()
    {
        var items = _fixture.CreateMany<int>(3).ToList();
        var response = new PagedResponse<int>(items, 2, 3, 9);

        var mapped = response.Map(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(items.Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)), mapped.Items);
        Assert.Equal(response.PageNumber, mapped.PageNumber);
        Assert.Equal(response.PageSize, mapped.PageSize);
        Assert.Equal(response.TotalCount, mapped.TotalCount);
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 10, -1)]
    public void Constructor_InvalidArguments_Throws(int pageNumber, int pageSize, int totalCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedResponse<int>([], pageNumber, pageSize, totalCount));
    }
}
