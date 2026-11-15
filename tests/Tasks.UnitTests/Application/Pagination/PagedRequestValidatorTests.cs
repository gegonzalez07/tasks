using Tasks.Application.Common.Pagination;
using Tasks.Application.Users;

namespace Tasks.UnitTests.Application.Pagination;

public sealed class PagedRequestValidatorTests
{
    private readonly PagedRequestValidator _validator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, PagedRequest.DefaultPageSize)]
    [InlineData(500, PagedRequest.MaxPageSize)]
    public void Validate_ValidValues_Passes(int pageNumber, int pageSize)
    {
        var result = _validator.Validate(new PagedRequest { PageNumber = pageNumber, PageSize = pageSize });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 10, nameof(PagedRequest.PageNumber))]
    [InlineData(1, 0, nameof(PagedRequest.PageSize))]
    [InlineData(1, PagedRequest.MaxPageSize + 1, nameof(PagedRequest.PageSize))]
    public void Validate_InvalidValues_Fails(int pageNumber, int pageSize, string property)
    {
        var result = _validator.Validate(new PagedRequest { PageNumber = pageNumber, PageSize = pageSize });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    [Fact]
    public void GetUsersRequestValidator_IncludesPaginationRules()
    {
        var result = new GetUsersRequestValidator().Validate(new GetUsersRequest { PageSize = 0, Search = new string('x', 101) });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetUsersRequest.PageSize));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetUsersRequest.Search));
    }
}
