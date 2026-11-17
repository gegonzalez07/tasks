using FluentValidation;

namespace Tasks.Application.TaskItems;

/// <summary>
/// Same field rules as creation, but the status is mandatory (full replacement).
/// "Due date not in the past" is checked by <see cref="TaskService"/>, because an overdue task must
/// remain editable (e.g. to mark it as done) as long as its due date is not changed.
/// </summary>
public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        TaskRequestRules.ApplyCommonRules(this, x => x.Title, x => x.Description, x => x.Assignee, x => x.Status, x => x.DueDate);

        RuleFor(x => x.Status)
            .NotNull();
    }
}
