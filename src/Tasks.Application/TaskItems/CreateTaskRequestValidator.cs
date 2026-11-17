using FluentValidation;

namespace Tasks.Application.TaskItems;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public const string DueDateInPastMessage = "'Due Date' must be today or a later date (UTC).";

    public CreateTaskRequestValidator()
        : this(TimeProvider.System)
    {
    }

    public CreateTaskRequestValidator(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        TaskRequestRules.ApplyCommonRules(this, x => x.Title, x => x.Description, x => x.Assignee, x => x.Status, x => x.DueDate);

        // A new task cannot be born overdue.
        RuleFor(x => x.DueDate)
            .Must(dueDate => dueDate >= TaskRequestRules.Today(clock))
            .When(x => x.DueDate.HasValue)
            .WithMessage(DueDateInPastMessage);
    }
}
