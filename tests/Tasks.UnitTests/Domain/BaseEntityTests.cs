using Tasks.Domain.Common;

namespace Tasks.UnitTests.Domain;

public sealed class BaseEntityTests
{
    [Fact]
    public void NewEntity_GetsTimeOrderedIdentifierAndDefaultState()
    {
        var entity = new SampleEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(7, entity.Id.Version);
        Assert.Equal(default, entity.CreatedAt);
        Assert.Null(entity.UpdatedAt);
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SampleEntity(Guid.Empty));
    }

    [Fact]
    public void Equality_IsBasedOnTypeAndId()
    {
        var id = Guid.CreateVersion7();

        var first = new SampleEntity(id);
        var second = new SampleEntity(id);
        var other = new OtherEntity(id);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.Equals(other));
        Assert.True(first != new SampleEntity());
    }

    private sealed class SampleEntity : BaseEntity
    {
        public SampleEntity()
        {
        }

        public SampleEntity(Guid id)
            : base(id)
        {
        }
    }

    private sealed class OtherEntity(Guid id) : BaseEntity(id);
}
