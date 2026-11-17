using Tasks.Application.Common.Pagination;

namespace Tasks.Application.TaskItems;

/// <summary>Task use cases. Missing (or soft-deleted) tasks raise <see cref="Domain.Exceptions.NotFoundException"/>.</summary>
public interface ITaskService
{
    Task<TaskDto> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<TaskDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResponse<TaskDto>> GetPagedAsync(GetTasksRequest request, CancellationToken cancellationToken = default);

    Task<TaskDto> UpdateAsync(UpdateTaskRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
