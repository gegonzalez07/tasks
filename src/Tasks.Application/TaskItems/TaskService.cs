using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Tasks.Application.Common.Pagination;
using Tasks.Application.Common.Time;
using Tasks.Domain.Exceptions;
using Tasks.Domain.Repositories;
using Tasks.Domain.TaskItems;

namespace Tasks.Application.TaskItems;

/// <summary>
/// Task use cases on top of <see cref="IUnitOfWork"/>. Input shape is validated by the request validators;
/// this service enforces the rules that need persisted state (existence, due-date change on update).
/// </summary>
public sealed class TaskService(IUnitOfWork unitOfWork, IClientCalendar calendar, ILogger<TaskService> logger) : ITaskService
{
    public const string ResourceName = "Task";
    public const string DueDateChangedToPastMessage =
        "'Due Date' can only be changed to today or a later date in the request time zone (X-Time-Zone header, UTC by default).";

    private IGenericRepository<TaskItem> Repository => unitOfWork.Repository<TaskItem>();

    public async Task<TaskDto> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = new TaskItem(
            request.Title,
            request.Description,
            request.Assignee,
            RequiredDueDate(request.DueDate),
            request.Status ?? TaskItemStatus.Pending);

        await Repository.AddAsync(task, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Task {TaskId} created for {Assignee}", task.Id, task.Assignee);
        return TaskDto.FromEntity(task);
    }

    public async Task<TaskDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        return TaskDto.FromEntity(task);
    }

    public Task<PagedResponse<TaskDto>> GetPagedAsync(GetTasksRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = Repository.Query();

        if (request.Status is { } status)
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Assignee))
        {
            // Case-insensitive on every provider (SQLite is case-sensitive by default).
            var assignee = request.Assignee.Trim().ToLower(System.Globalization.CultureInfo.InvariantCulture);
#pragma warning disable CA1304, CA1311, CA1862 // ToLower() without culture is required for SQL translation.
            query = query.Where(t => t.Assignee.ToLower() == assignee);
#pragma warning restore CA1304, CA1311, CA1862
        }

        // Most urgent first; CreatedAt and Id make the order (and therefore the pages) deterministic.
        return query
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .Select(TaskDto.Projection)
            .ToPagedResponseAsync(request, cancellationToken);
    }

    public async Task<TaskDto> UpdateAsync(UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = await FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        var dueDate = RequiredDueDate(request.DueDate);

        // An overdue task stays editable (e.g. to mark it as done), but its due date cannot be moved into the past.
        if (dueDate != task.DueDate && dueDate < calendar.Today)
        {
            throw new ValidationException(new[] { new ValidationFailure(nameof(UpdateTaskRequest.DueDate), DueDateChangedToPastMessage) });
        }

        task.Update(
            request.Title,
            request.Description,
            request.Assignee,
            dueDate,
            request.Status ?? task.Status);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Task {TaskId} updated", task.Id);
        return TaskDto.FromEntity(task);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await FindAsync(id, cancellationToken).ConfigureAwait(false);

        Repository.Remove(task); // soft delete (applied by AppDbContext)
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Task {TaskId} deleted", task.Id);
    }

    private async Task<TaskItem> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await Repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException(ResourceName, id);

    private static DateOnly RequiredDueDate(DateOnly? dueDate) =>
        dueDate ?? throw new ValidationException(new[] { new ValidationFailure("DueDate", "'Due Date' must not be empty.") });
}
