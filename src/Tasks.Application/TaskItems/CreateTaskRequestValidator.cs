using FluentValidation;
using Tasks.Application.Common.Time;

namespace Tasks.Application.TaskItems;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public const string DueDateInPastMessage =
        "'Due Date' must be today or a later date in the request time zone (X-Time-Zone header, UTC by default).";

    /// <summary>
    /// Single constructor on purpose: FastEndpoints (and the DI container) build validators with
    /// ActivatorUtilities, which fails when more than one constructor can be satisfied.
    /// <see cref="IClientCalendar"/> is registered in <c>AddApplication</c>.
    /// </summary>
    public CreateTaskRequestValidator(IClientCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        TaskRequestRules.ApplyCommonRules(this, x => x.Title, x => x.Description, x => x.Assignee, x => x.Status, x => x.DueDate);

        // A new task cannot be born overdue. "Today" is the caller's date, not the server's UTC date.
        RuleFor(x => x.DueDate)
            .Must(dueDate => dueDate >= calendar.Today)
            .When(x => x.DueDate.HasValue)
            .WithMessage(DueDateInPastMessage);
    }
}
