using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tasks.Application.Common.Pagination;
using Tasks.Infrastructure.Persistence.Repositories;

namespace Tasks.UnitTests.Infrastructure.Persistence;

public sealed class UnitOfWorkTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly Fixture _fixture = new();
    private readonly Mock<TimeProvider> _clock = new();
    private readonly SqliteTestDatabase _database;

    public UnitOfWorkTests()
    {
        _clock.Setup(c => c.GetUtcNow()).Returns(T0);
        _database = new SqliteTestDatabase(_clock.Object);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SaveChangesAsync_NewEntity_StampsCreationAudit()
    {
        await using var context = _database.CreateContext();
        var sut = new UnitOfWork(context);
        var item = new TestItem(_fixture.Create<string>());

        await sut.Repository<TestItem>().AddAsync(item);
        await sut.SaveChangesAsync();

        Assert.Equal(T0.UtcDateTime, item.CreatedAt);
        Assert.Null(item.UpdatedAt);
        Assert.False(item.IsDeleted);
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedEntity_StampsUpdateAndPreservesCreation()
    {
        var id = await SeedAsync(1);
        var later = T0.AddHours(3);
        _clock.Setup(c => c.GetUtcNow()).Returns(later);

        await using (var context = _database.CreateContext())
        {
            var sut = new UnitOfWork(context);
            var item = await sut.Repository<TestItem>().GetByIdAsync(id[0]);
            item!.Name = "renamed";
            await sut.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();
        var stored = await verify.Set<TestItem>().SingleAsync(i => i.Id == id[0]);
        Assert.Equal("renamed", stored.Name);
        Assert.Equal(T0.UtcDateTime, stored.CreatedAt);
        Assert.Equal(later.UtcDateTime, stored.UpdatedAt);
    }

    [Fact]
    public async Task Remove_PerformsSoftDelete()
    {
        var ids = await SeedAsync(2);

        await using (var context = _database.CreateContext())
        {
            var sut = new UnitOfWork(context);
            var repository = sut.Repository<TestItem>();
            repository.Remove((await repository.GetByIdAsync(ids[0]))!);
            await sut.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();
        var verifyUow = new UnitOfWork(verify);
        Assert.Null(await verifyUow.Repository<TestItem>().GetByIdAsync(ids[0]));
        Assert.Equal(1, await verifyUow.Repository<TestItem>().CountAsync());

        var raw = await verify.Set<TestItem>().IgnoreQueryFilters().SingleAsync(i => i.Id == ids[0]);
        Assert.True(raw.IsDeleted);
        Assert.NotNull(raw.UpdatedAt);
    }

    [Fact]
    public async Task Repository_ReturnsSameInstancePerEntityType()
    {
        await using var context = _database.CreateContext();
        var sut = new UnitOfWork(context);

        Assert.Same(sut.Repository<TestItem>(), sut.Repository<TestItem>());
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_Success_CommitsAllChanges()
    {
        await using (var context = _database.CreateContext())
        {
            var sut = new UnitOfWork(context);

            var created = await sut.ExecuteInTransactionAsync(async ct =>
            {
                var items = _fixture.CreateMany<string>(3).Select(n => new TestItem(n)).ToList();
                await sut.Repository<TestItem>().AddRangeAsync(items, ct);
                return items.Count;
            });

            Assert.Equal(3, created);
            Assert.False(sut.HasActiveTransaction);
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(3, await verify.Set<TestItem>().CountAsync());
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_Failure_RollsBackAndRethrows()
    {
        await using (var context = _database.CreateContext())
        {
            var sut = new UnitOfWork(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteInTransactionAsync(async ct =>
            {
                await sut.Repository<TestItem>().AddAsync(new TestItem("first"), ct);
                await sut.SaveChangesAsync(ct); // flushed inside the transaction...
                throw new InvalidOperationException("boom"); // ...and must be rolled back
            }));

            Assert.False(sut.HasActiveTransaction);
            Assert.Empty(context.ChangeTracker.Entries());
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(0, await verify.Set<TestItem>().CountAsync());
    }

    [Fact]
    public async Task ManualTransaction_Rollback_DiscardsSavedChanges()
    {
        await using (var context = _database.CreateContext())
        {
            var sut = new UnitOfWork(context);
            await sut.BeginTransactionAsync();
            await sut.Repository<TestItem>().AddAsync(new TestItem("temp"));
            await sut.SaveChangesAsync();

            await sut.RollbackTransactionAsync();

            Assert.False(sut.HasActiveTransaction);
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(0, await verify.Set<TestItem>().CountAsync());
    }

    [Fact]
    public async Task ManualTransaction_Commit_PersistsPendingChanges()
    {
        await using (var context = _database.CreateContext())
        {
            var sut = new UnitOfWork(context);
            await sut.BeginTransactionAsync();
            Assert.True(sut.HasActiveTransaction);

            await sut.Repository<TestItem>().AddAsync(new TestItem("kept"));
            await sut.CommitTransactionAsync(); // saves + commits

            Assert.False(sut.HasActiveTransaction);
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(1, await verify.Set<TestItem>().CountAsync());
    }

    [Fact]
    public async Task TransactionMisuse_Throws()
    {
        await using var context = _database.CreateContext();
        var sut = new UnitOfWork(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CommitTransactionAsync());

        await sut.BeginTransactionAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.BeginTransactionAsync());
        await sut.RollbackTransactionAsync();
    }

    [Fact]
    public async Task ToPagedResponseAsync_OnEfQuery_UsesDatabaseAndExcludesSoftDeleted()
    {
        var ids = await SeedAsync(25);
        await using (var context = _database.CreateContext())
        {
            var sut = new UnitOfWork(context);
            var repository = sut.Repository<TestItem>();
            repository.Remove((await repository.GetByIdAsync(ids[0]))!);
            await sut.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();
        var page = await new UnitOfWork(verify).Repository<TestItem>()
            .Query()
            .OrderBy(i => i.Name)
            .ToPagedResponseAsync(new PagedRequest { PageNumber = 3, PageSize = 10 });

        Assert.Equal(24, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(4, page.Items.Count);
        Assert.DoesNotContain(page.Items, i => i.Id == ids[0]);
    }

    private async Task<List<Guid>> SeedAsync(int count)
    {
        await using var context = _database.CreateContext();
        var items = Enumerable.Range(1, count).Select(i => new TestItem($"item-{i:D3}")).ToList();
        context.AddRange(items);
        await context.SaveChangesAsync();
        return items.Select(i => i.Id).ToList();
    }
}
