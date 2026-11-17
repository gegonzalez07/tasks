using FluentValidation;
using Tasks.Application.Common.Pagination;
using Tasks.Domain.TaskItems;

namespace Tasks.Application.TaskItems;

public sealed class GetTasksRequestValidator : AbstractValidator<GetTasksRequest>
{
    public GetTasksRequestValidator()
    {
        Include(new PagedRequestValidator());

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Assignee)
            .MaximumLength(TaskItem.AssigneeMaxLength);
    }
}
