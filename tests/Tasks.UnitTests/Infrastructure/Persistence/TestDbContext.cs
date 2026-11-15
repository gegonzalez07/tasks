using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tasks.Domain.Common;
using Tasks.Infrastructure.Persistence;

namespace Tasks.UnitTests.Infrastructure.Persistence;

/// <summary>Sample entity used to exercise the generic persistence infrastructure.</summary>
public sealed class TestItem : BaseEntity
{
    public TestItem(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}

/// <summary><see cref="AppDbContext"/> plus a test entity, so the real auditing / soft-delete logic is exercised.</summary>
public sealed class TestDbContext(DbContextOptions<TestDbContext> options, TimeProvider timeProvider)
    : AppDbContext(options, timeProvider)
{
    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.Entity<TestItem>(item =>
        {
            item.ToTable("TestItems");
            item.Property(i => i.Name).HasMaxLength(100).IsRequired();
        });
    }
}

/// <summary>
/// SQLite in-memory database (real relational provider with transactions) shared by several
/// <see cref="TestDbContext"/> instances through a single open connection.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteTestDatabase(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public TimeProvider TimeProvider { get; }

    public TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new TestDbContext(options, TimeProvider);
    }

    public void Dispose() => _connection.Dispose();
}
