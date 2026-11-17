using Tasks.Domain.TaskItems;

namespace Tasks.Application.TaskItems;

/// <summary>Full replacement of a task (PUT semantics). <see cref="Id"/> comes from the route.</summary>
public sealed class UpdateTaskRequest
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskItemStatus? Status { get; set; }

    public string Assignee { get; set; } = string.Empty;

    public DateOnly? DueDate { get; set; }
}
