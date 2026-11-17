using AutoFixture;
using Tasks.Domain.Exceptions;
using Tasks.Domain.TaskItems;

namespace Tasks.UnitTests.Domain;

public sealed class TaskItemTests
{
    private static readonly DateOnly DueDate = new(2026, 12, 31);
    private readonly Fixture _fixture = new();

    [Fact]
    public void Constructor_ValidData_CreatesPendingTaskWithTrimmedValues()
    {
        var task = new TaskItem("  Write README  ", "  Explain decisions  ", "  ana  ", DueDate);

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Write README", task.Title);
        Assert.Equal("Explain decisions", task.Description);
        Assert.Equal("ana", task.Assignee);
        Assert.Equal(DueDate, task.DueDate);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.False(task.IsDeleted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankDescription_IsStoredAsNull(string? description)
    {
        var task = new TaskItem("Title", description, "ana", DueDate);

        Assert.Null(task.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_MissingTitle_Throws(string? title)
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(() => new TaskItem(title!, null, "ana", DueDate));

        Assert.Equal("task.title.required", exception.Rule);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Constructor_MissingAssignee_Throws(string? assignee)
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(() => new TaskItem("Title", null, assignee!, DueDate));

        Assert.Equal("task.assignee.required", exception.Rule);
    }

    [Theory]
    [InlineData(TaskItem.TitleMaxLength + 1, 1, 1, "task.title.too_long")]
    [InlineData(1, TaskItem.DescriptionMaxLength + 1, 1, "task.description.too_long")]
    [InlineData(1, 1, TaskItem.AssigneeMaxLength + 1, "task.assignee.too_long")]
    public void Constructor_TooLongValues_Throw(int titleLength, int descriptionLength, int assigneeLength, string rule)
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(() => new TaskItem(
            new string('t', titleLength),
            new string('d', descriptionLength),
            new string('a', assigneeLength),
            DueDate));

        Assert.Equal(rule, exception.Rule);
    }

    [Fact]
    public void Constructor_ValuesAtMaxLength_AreAccepted()
    {
        var task = new TaskItem(
            new string('t', TaskItem.TitleMaxLength),
            new string('d', TaskItem.DescriptionMaxLength),
            new string('a', TaskItem.AssigneeMaxLength),
            DueDate);

        Assert.Equal(TaskItem.TitleMaxLength, task.Title.Length);
    }

    [Fact]
    public void Constructor_UndefinedStatus_Throws()
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => new TaskItem("Title", null, "ana", DueDate, (TaskItemStatus)99));

        Assert.Equal("task.status.invalid", exception.Rule);
    }

    [Fact]
    public void Update_ReplacesAllEditableFields()
    {
        var task = new TaskItem(_fixture.Create<string>(), _fixture.Create<string>(), "ana", DueDate);
        var newDueDate = DueDate.AddDays(7);

        task.Update("New title", null, "luis", newDueDate, TaskItemStatus.InProgress);

        Assert.Equal("New title", task.Title);
        Assert.Null(task.Description);
        Assert.Equal("luis", task.Assignee);
        Assert.Equal(newDueDate, task.DueDate);
        Assert.Equal(TaskItemStatus.InProgress, task.Status);
    }

    [Fact]
    public void Update_InvalidData_LeavesTaskUnchanged()
    {
        var task = new TaskItem("Original", "Description", "ana", DueDate);

        Assert.Throws<BusinessRuleViolationException>(
            () => task.Update("Changed", "Changed", " ", DueDate.AddDays(1), TaskItemStatus.Done));

        Assert.Equal("Original", task.Title);
        Assert.Equal("Description", task.Description);
        Assert.Equal("ana", task.Assignee);
        Assert.Equal(DueDate, task.DueDate);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
    }

    [Theory]
    [InlineData(TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.Pending)]
    public void ChangeStatus_ValidStatus_UpdatesStatus(TaskItemStatus status)
    {
        var task = new TaskItem("Title", null, "ana", DueDate);

        task.ChangeStatus(status);

        Assert.Equal(status, task.Status);
    }

    [Fact]
    public void ChangeStatus_UndefinedStatus_Throws()
    {
        var task = new TaskItem("Title", null, "ana", DueDate);

        Assert.Throws<BusinessRuleViolationException>(() => task.ChangeStatus((TaskItemStatus)(-1)));
        Assert.Equal(TaskItemStatus.Pending, task.Status);
    }
}
