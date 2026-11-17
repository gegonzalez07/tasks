using Moq;
using Tasks.Application.TaskItems;
using Tasks.Domain.TaskItems;

namespace Tasks.UnitTests.Application.TaskItems;

public sealed class TaskRequestValidatorsTests
{
    private static readonly DateOnly Today = new(2026, 11, 17);
    private readonly CreateTaskRequestValidator _createValidator;
    private readonly UpdateTaskRequestValidator _updateValidator = new();
    private readonly GetTasksRequestValidator _listValidator = new();

    public TaskRequestValidatorsTests()
    {
        var clock = new Mock<TimeProvider>();
        clock.Setup(c => c.GetUtcNow()).Returns(new DateTimeOffset(2026, 11, 17, 23, 59, 0, TimeSpan.Zero));
        _createValidator = new CreateTaskRequestValidator(clock.Object);
    }

    [Fact]
    public void Create_ValidRequest_Passes()
    {
        var result = _createValidator.Validate(ValidCreate());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_DueDateToday_Passes()
    {
        var request = ValidCreate();
        request.DueDate = Today;

        Assert.True(_createValidator.Validate(request).IsValid);
    }

    [Fact]
    public void Create_DueDateInThePast_Fails()
    {
        var request = ValidCreate();
        request.DueDate = Today.AddDays(-1);

        var result = _createValidator.Validate(request);

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateTaskRequest.DueDate), error.PropertyName);
        Assert.Equal(CreateTaskRequestValidator.DueDateInPastMessage, error.ErrorMessage);
    }

    [Theory]
    [InlineData("", "ana", nameof(CreateTaskRequest.Title))]
    [InlineData("   ", "ana", nameof(CreateTaskRequest.Title))]
    [InlineData("Title", "", nameof(CreateTaskRequest.Assignee))]
    public void Create_MissingRequiredText_Fails(string title, string assignee, string property)
    {
        var request = ValidCreate();
        request.Title = title;
        request.Assignee = assignee;

        var result = _createValidator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    [Fact]
    public void Create_MissingDueDate_Fails()
    {
        var request = ValidCreate();
        request.DueDate = null;

        var result = _createValidator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskRequest.DueDate));
    }

    [Fact]
    public void Create_TooLongValues_Fail()
    {
        var request = ValidCreate();
        request.Title = new string('t', TaskItem.TitleMaxLength + 1);
        request.Description = new string('d', TaskItem.DescriptionMaxLength + 1);
        request.Assignee = new string('a', TaskItem.AssigneeMaxLength + 1);

        var result = _createValidator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Title));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Description));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Assignee));
    }

    [Fact]
    public void Create_UndefinedStatus_Fails()
    {
        var request = ValidCreate();
        request.Status = (TaskItemStatus)42;

        Assert.Contains(_createValidator.Validate(request).Errors, e => e.PropertyName == nameof(CreateTaskRequest.Status));
    }

    [Fact]
    public void Create_WithoutStatus_Passes()
    {
        var request = ValidCreate();
        request.Status = null;

        Assert.True(_createValidator.Validate(request).IsValid);
    }

    [Fact]
    public void Update_ValidRequestWithPastDueDate_PassesBecauseServiceDecides()
    {
        var request = ValidUpdate();
        request.DueDate = Today.AddDays(-30);

        Assert.True(_updateValidator.Validate(request).IsValid);
    }

    [Fact]
    public void Update_MissingStatus_Fails()
    {
        var request = ValidUpdate();
        request.Status = null;

        Assert.Contains(_updateValidator.Validate(request).Errors, e => e.PropertyName == nameof(UpdateTaskRequest.Status));
    }

    [Fact]
    public void Update_MissingTitleAndDueDate_Fail()
    {
        var request = ValidUpdate();
        request.Title = " ";
        request.DueDate = null;

        var result = _updateValidator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTaskRequest.Title));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTaskRequest.DueDate));
    }

    [Fact]
    public void List_ValidFilters_Pass()
    {
        var request = new GetTasksRequest { Status = TaskItemStatus.Done, Assignee = "ana", PageNumber = 2, PageSize = 50 };

        Assert.True(_listValidator.Validate(request).IsValid);
    }

    [Fact]
    public void List_InvalidValues_Fail()
    {
        var request = new GetTasksRequest
        {
            Status = (TaskItemStatus)7,
            Assignee = new string('a', TaskItem.AssigneeMaxLength + 1),
            PageSize = 0,
        };

        var result = _listValidator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTasksRequest.Status));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTasksRequest.Assignee));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTasksRequest.PageSize));
    }

    private static CreateTaskRequest ValidCreate() => new()
    {
        Title = "Write README",
        Description = "Explain decisions",
        Assignee = "ana",
        Status = TaskItemStatus.Pending,
        DueDate = Today.AddDays(3),
    };

    private static UpdateTaskRequest ValidUpdate() => new()
    {
        Id = Guid.CreateVersion7(),
        Title = "Write README",
        Description = null,
        Assignee = "ana",
        Status = TaskItemStatus.InProgress,
        DueDate = Today.AddDays(3),
    };
}
