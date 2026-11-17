using Microsoft.EntityFrameworkCore;
using Moq;
using Tasks.Domain.TaskItems;

namespace Tasks.UnitTests.Infrastructure.Persistence;

/// <summary>Verifies the EF Core mapping of <see cref="TaskItem"/> against a real relational provider (SQLite).</summary>
public sealed class TaskItemPersistenceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);
    private readonly SqliteTestDatabase _database;

    public TaskItemPersistenceTests()
    {
        var clock = new Mock<TimeProvider>();
        clock.Setup(c => c.GetUtcNow()).Returns(Now);
        _database = new SqliteTestDatabase(clock.Object);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SaveAndReload_RoundTripsEveryField()
    {
        var task = new TaskItem("Write README", "Explain decisions", "ana", new DateOnly(2026, 12, 1), TaskItemStatus.InProgress);
        await using (var context = _database.CreateContext())
        {
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();
        var stored = await verify.Tasks.SingleAsync(t => t.Id == task.Id);

        Assert.Equal("Write README", stored.Title);
        Assert.Equal("Explain decisions", stored.Description);
        Assert.Equal("ana", stored.Assignee);
        Assert.Equal(new DateOnly(2026, 12, 1), stored.DueDate);
        Assert.Equal(TaskItemStatus.InProgress, stored.Status);
        Assert.Equal(Now.UtcDateTime, stored.CreatedAt);
    }

    [Fact]
    public async Task Status_IsStoredAsText()
    {
        var task = new TaskItem("Title", null, "ana", new DateOnly(2026, 12, 1), TaskItemStatus.Done);
        await using var context = _database.CreateContext();
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        // Fresh database per test: the only row is the one just saved.
        var raw = await context.Database
            .SqlQuery<string>($"SELECT Status AS Value FROM Tasks")
            .SingleAsync();

        Assert.Equal("Done", raw);
    }

    [Fact]
    public async Task Remove_SoftDeletesAndHidesTheTask()
    {
        var task = new TaskItem("Title", null, "ana", new DateOnly(2026, 12, 1));
        await using (var context = _database.CreateContext())
        {
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
            context.Tasks.Remove(task);
            await context.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();
        Assert.False(await verify.Tasks.AnyAsync(t => t.Id == task.Id));
        Assert.True(await verify.Tasks.IgnoreQueryFilters().AnyAsync(t => t.Id == task.Id && t.IsDeleted));
    }
}
