using System.Linq.Expressions;
using Tasks.Domain.TaskItems;

namespace Tasks.Application.TaskItems;

/// <summary>Task representation returned by the API.</summary>
public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    string Assignee,
    DateOnly DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    /// <summary>Projection translated to SQL by EF Core (only the needed columns are read).</summary>
    public static Expression<Func<TaskItem, TaskDto>> Projection { get; } = task => new TaskDto(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.Assignee,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);

    private static readonly Func<TaskItem, TaskDto> CompiledProjection = Projection.Compile();

    public static TaskDto FromEntity(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return CompiledProjection(task);
    }
}
