using FluentValidation;
using Tasks.Application.Common.Pagination;

namespace Tasks.Application.Users;

public sealed class GetUsersRequestValidator : AbstractValidator<GetUsersRequest>
{
    public GetUsersRequestValidator()
    {
        Include(new PagedRequestValidator());

        RuleFor(x => x.Search)
            .MaximumLength(100);
    }
}
