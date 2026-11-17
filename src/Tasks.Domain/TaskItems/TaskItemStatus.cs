namespace Tasks.Domain.TaskItems;

/// <summary>Lifecycle state of a <see cref="TaskItem"/>.</summary>
public enum TaskItemStatus
{
    Pending = 0,
    InProgress = 1,
    Done = 2,
}
