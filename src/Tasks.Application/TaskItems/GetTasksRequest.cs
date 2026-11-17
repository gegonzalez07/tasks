using Tasks.Application.Common.Pagination;
using Tasks.Domain.TaskItems;

namespace Tasks.Application.TaskItems;

/// <summary>Paged task list with optional filters (combined with AND).</summary>
public sealed class GetTasksRequest : PagedRequest
{
    public TaskItemStatus? Status { get; set; }

    /// <summary>Exact assignee match, case-insensitive.</summary>
    public string? Assignee { get; set; }
}
