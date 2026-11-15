namespace Tasks.Domain.Common;

/// <summary>
/// Base type for every persisted domain entity.
/// Audit fields (<see cref="CreatedAt"/>, <see cref="UpdatedAt"/>) and the soft-delete flag
/// (<see cref="IsDeleted"/>) are maintained automatically by the persistence layer;
/// domain code never sets them directly.
/// </summary>
public abstract class BaseEntity : IEquatable<BaseEntity>
{
    protected BaseEntity()
        : this(Guid.CreateVersion7())
    {
    }

    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("The entity identifier cannot be empty.", nameof(id));
        }

        Id = id;
    }

    /// <summary>Time-ordered identifier (UUID v7), index friendly.</summary>
    public Guid Id { get; private set; }

    /// <summary>UTC instant when the entity was first persisted.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC instant of the last persisted modification (null if never modified).</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Soft-delete flag. Deleted entities are hidden by a global query filter.</summary>
    public bool IsDeleted { get; private set; }

    public bool Equals(BaseEntity? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || (GetType() == other.GetType() && Id == other.Id);
    }

    public override bool Equals(object? obj) => obj is BaseEntity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(BaseEntity? left, BaseEntity? right) => Equals(left, right);

    public static bool operator !=(BaseEntity? left, BaseEntity? right) => !Equals(left, right);
}
