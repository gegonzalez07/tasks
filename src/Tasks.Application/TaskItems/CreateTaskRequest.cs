using Tasks.Domain.TaskItems;

namespace Tasks.Application.TaskItems;

public sealed class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Optional; defaults to <see cref="TaskItemStatus.Pending"/>.</summary>
    public TaskItemStatus? Status { get; set; }

    public string Assignee { get; set; } = string.Empty;

    /// <summary>Required. Nullable so a missing value is reported as a validation error instead of 0001-01-01.</summary>
    public DateOnly? DueDate { get; set; }
}
