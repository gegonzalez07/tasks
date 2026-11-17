using Tasks.Domain.Common;
using Tasks.Domain.Exceptions;

namespace Tasks.Domain.TaskItems;

/// <summary>
/// A unit of work assigned to someone, with a due date and a lifecycle status.
/// Named <c>TaskItem</c> to avoid clashing with <see cref="System.Threading.Tasks.Task"/>.
/// The entity protects its own invariants (required fields, lengths, valid status); rules that
/// depend on the current time (e.g. "due date not in the past") belong to the application layer.
/// </summary>
public sealed class TaskItem : BaseEntity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int AssigneeMaxLength = 100;

    public TaskItem(
        string title,
        string? description,
        string assignee,
        DateOnly dueDate,
        TaskItemStatus status = TaskItemStatus.Pending)
    {
        Title = string.Empty;
        Assignee = string.Empty;
        Update(title, description, assignee, dueDate, status);
    }

    /// <summary>For EF Core materialisation.</summary>
    private TaskItem()
    {
        Title = string.Empty;
        Assignee = string.Empty;
    }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    /// <summary>Name or identifier of the person responsible for the task.</summary>
    public string Assignee { get; private set; }

    public DateOnly DueDate { get; private set; }

    /// <summary>Replaces every editable field (PUT semantics). Values are trimmed.</summary>
    public void Update(string title, string? description, string assignee, DateOnly dueDate, TaskItemStatus status)
    {
        var normalizedTitle = Required(title, nameof(Title), TitleMaxLength);
        var normalizedAssignee = Required(assignee, nameof(Assignee), AssigneeMaxLength);
        var normalizedDescription = Optional(description, nameof(Description), DescriptionMaxLength);
        EnsureValid(status);

        Title = normalizedTitle;
        Description = normalizedDescription;
        Assignee = normalizedAssignee;
        DueDate = dueDate;
        Status = status;
    }

    public void ChangeStatus(TaskItemStatus status)
    {
        EnsureValid(status);
        Status = status;
    }

    private static string Required(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new BusinessRuleViolationException($"task.{field.ToLowerInvariant()}.required", $"{field} is required.");
        }

        EnsureMaxLength(trimmed, field, maxLength);
        return trimmed;
    }

    private static string? Optional(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        EnsureMaxLength(trimmed, field, maxLength);
        return trimmed;
    }

    private static void EnsureMaxLength(string value, string field, int maxLength)
    {
        if (value.Length > maxLength)
        {
            throw new BusinessRuleViolationException(
                $"task.{field.ToLowerInvariant()}.too_long",
                $"{field} must be at most {maxLength} characters.");
        }
    }

    private static void EnsureValid(TaskItemStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new BusinessRuleViolationException("task.status.invalid", $"'{(int)status}' is not a valid task status.");
        }
    }
}
