using AutoFixture;
using AutoFixture.AutoMoq;
using FluentValidation;
using Moq;
using Tasks.Application.Common.Time;
using Tasks.Application.TaskItems;
using Tasks.Domain.Exceptions;
using Tasks.Domain.Repositories;
using Tasks.Domain.TaskItems;

namespace Tasks.UnitTests.Application.TaskItems;

public sealed class TaskServiceTests
{
    private static readonly DateOnly Today = new(2026, 11, 17);

    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
    private readonly Mock<IGenericRepository<TaskItem>> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly List<TaskItem> _store = [];
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        var calendar = new Mock<IClientCalendar>();
        calendar.Setup(c => c.Today).Returns(Today);
        _fixture.Inject(calendar.Object);

        _repository = _fixture.Freeze<Mock<IGenericRepository<TaskItem>>>();
        _repository.Setup(r => r.Query(It.IsAny<bool>())).Returns(() => _store.AsQueryable());
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => _store.SingleOrDefault(t => t.Id == id));

        _unitOfWork = _fixture.Freeze<Mock<IUnitOfWork>>();
        _unitOfWork.Setup(u => u.Repository<TaskItem>()).Returns(_repository.Object);

        _sut = _fixture.Create<TaskService>();
    }

    [Fact]
    public async Task CreateAsync_PersistsTaskAndReturnsDto()
    {
        var request = new CreateTaskRequest
        {
            Title = _fixture.Create<string>(),
            Description = _fixture.Create<string>(),
            Assignee = "ana",
            DueDate = Today.AddDays(2),
        };
        TaskItem? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((task, _) => added = task)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(added);
        Assert.Equal(added.Id, result.Id);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal("ana", result.Assignee);
        Assert.Equal(Today.AddDays(2), result.DueDate);
        Assert.Equal(TaskItemStatus.Pending, result.Status); // default when not provided
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithStatus_UsesIt()
    {
        var result = await _sut.CreateAsync(new CreateTaskRequest
        {
            Title = "Title",
            Assignee = "ana",
            Status = TaskItemStatus.InProgress,
            DueDate = Today,
        });

        Assert.Equal(TaskItemStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDto()
    {
        var task = Seed("Existing", "ana", Today, TaskItemStatus.Done);

        var result = await _sut.GetByIdAsync(task.Id);

        Assert.Equal(task.Id, result.Id);
        Assert.Equal("Existing", result.Title);
        Assert.Equal(TaskItemStatus.Done, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ThrowsNotFound()
    {
        var id = Guid.CreateVersion7();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(id));

        Assert.Equal(id, exception.Key);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByStatusAndAssigneeCaseInsensitively()
    {
        Seed("A", "Ana", Today.AddDays(1), TaskItemStatus.Pending);
        Seed("B", "ana", Today.AddDays(2), TaskItemStatus.Done);
        Seed("C", "luis", Today.AddDays(3), TaskItemStatus.Pending);
        Seed("D", "ANA", Today.AddDays(4), TaskItemStatus.Pending);

        var result = await _sut.GetPagedAsync(new GetTasksRequest { Status = TaskItemStatus.Pending, Assignee = " ana " });

        Assert.Equal(new[] { "A", "D" }, result.Items.Select(t => t.Title));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_OrdersByDueDateAndPaginates()
    {
        Seed("Later", "ana", Today.AddDays(9), TaskItemStatus.Pending);
        Seed("Sooner", "ana", Today.AddDays(1), TaskItemStatus.Pending);
        Seed("Middle", "ana", Today.AddDays(5), TaskItemStatus.Pending);

        var firstPage = await _sut.GetPagedAsync(new GetTasksRequest { PageNumber = 1, PageSize = 2 });
        var secondPage = await _sut.GetPagedAsync(new GetTasksRequest { PageNumber = 2, PageSize = 2 });

        Assert.Equal(new[] { "Sooner", "Middle" }, firstPage.Items.Select(t => t.Title));
        Assert.Equal(new[] { "Later" }, secondPage.Items.Select(t => t.Title));
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.True(firstPage.HasNextPage);
        Assert.False(secondPage.HasNextPage);
    }

    [Fact]
    public async Task UpdateAsync_Existing_ReplacesFieldsAndSaves()
    {
        var task = Seed("Old", "ana", Today.AddDays(1), TaskItemStatus.Pending);

        var result = await _sut.UpdateAsync(new UpdateTaskRequest
        {
            Id = task.Id,
            Title = "New",
            Description = "Details",
            Assignee = "luis",
            Status = TaskItemStatus.InProgress,
            DueDate = Today.AddDays(10),
        });

        Assert.Equal("New", result.Title);
        Assert.Equal("luis", task.Assignee);
        Assert.Equal(TaskItemStatus.InProgress, task.Status);
        Assert.Equal(Today.AddDays(10), task.DueDate);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_OverdueTaskKeepingItsDueDate_IsAllowed()
    {
        var overdue = Seed("Overdue", "ana", Today.AddDays(-5), TaskItemStatus.InProgress);

        var result = await _sut.UpdateAsync(new UpdateTaskRequest
        {
            Id = overdue.Id,
            Title = "Overdue",
            Assignee = "ana",
            Status = TaskItemStatus.Done,
            DueDate = Today.AddDays(-5),
        });

        Assert.Equal(TaskItemStatus.Done, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_MovingDueDateIntoThePast_ThrowsValidationAndDoesNotSave()
    {
        var task = Seed("Task", "ana", Today.AddDays(3), TaskItemStatus.Pending);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.UpdateAsync(new UpdateTaskRequest
        {
            Id = task.Id,
            Title = "Task",
            Assignee = "ana",
            Status = TaskItemStatus.Pending,
            DueDate = Today.AddDays(-1),
        }));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(UpdateTaskRequest.DueDate));
        Assert.Equal(Today.AddDays(3), task.DueDate);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(new UpdateTaskRequest
        {
            Id = Guid.CreateVersion7(),
            Title = "Title",
            Assignee = "ana",
            Status = TaskItemStatus.Pending,
            DueDate = Today,
        }));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Existing_RemovesAndSaves()
    {
        var task = Seed("Task", "ana", Today, TaskItemStatus.Pending);

        await _sut.DeleteAsync(task.Id);

        _repository.Verify(r => r.Remove(task), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Missing_ThrowsNotFoundAndDoesNotSave()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.CreateVersion7()));

        _repository.Verify(r => r.Remove(It.IsAny<TaskItem>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private TaskItem Seed(string title, string assignee, DateOnly dueDate, TaskItemStatus status)
    {
        var task = new TaskItem(title, null, assignee, dueDate, status);
        _store.Add(task);
        return task;
    }
}
