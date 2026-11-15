using FluentValidation;

namespace Tasks.Application.Common.Pagination;

/// <summary>
/// Reusable pagination rules. Include it from validators of requests deriving from <see cref="PagedRequest"/>:
/// <c>Include(new PagedRequestValidator());</c>
/// </summary>
public sealed class PagedRequestValidator : AbstractValidator<PagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(int.MaxValue / PagedRequest.MaxPageSize);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PagedRequest.MaxPageSize);
    }
}
