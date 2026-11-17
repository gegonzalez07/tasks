using FluentValidation;
using Tasks.Domain.TaskItems;

namespace Tasks.Application.TaskItems;

/// <summary>Field rules shared by the create and update validators (same limits as the entity).</summary>
internal static class TaskRequestRules
{
    public static void ApplyCommonRules<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> title,
        System.Linq.Expressions.Expression<Func<T, string?>> description,
        System.Linq.Expressions.Expression<Func<T, string>> assignee,
        System.Linq.Expressions.Expression<Func<T, TaskItemStatus?>> status,
        System.Linq.Expressions.Expression<Func<T, DateOnly?>> dueDate)
    {
        validator.RuleFor(title)
            .NotEmpty()
            .MaximumLength(TaskItem.TitleMaxLength);

        validator.RuleFor(description)
            .MaximumLength(TaskItem.DescriptionMaxLength);

        validator.RuleFor(assignee)
            .NotEmpty()
            .MaximumLength(TaskItem.AssigneeMaxLength);

        validator.RuleFor(status)
            .IsInEnum();

        validator.RuleFor(dueDate)
            .NotNull();
    }
}
