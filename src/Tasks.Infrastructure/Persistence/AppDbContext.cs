using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tasks.Domain.Common;
using Tasks.Infrastructure.Identity;

namespace Tasks.Infrastructure.Persistence;

/// <summary>
/// EF Core context: ASP.NET Core Identity tables plus every domain entity configured in this assembly.
/// Applies automatic auditing and soft delete for <see cref="BaseEntity"/> types.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly TimeProvider _timeProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider timeProvider)
        : base(options)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>For derived contexts (e.g. tests that add extra entities).</summary>
    protected AppDbContext(DbContextOptions options, TimeProvider timeProvider)
        : base(options)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditing();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureModel(builder);
        ApplyBaseEntityConventions(builder);
    }

    /// <summary>Extension point for derived contexts; runs before the soft-delete conventions are applied.</summary>
    protected virtual void ConfigureModel(ModelBuilder builder)
    {
    }

    /// <summary>Stamps audit fields and turns hard deletes into soft deletes.</summary>
    private void ApplyAuditing()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAt).CurrentValue = now;
                    entry.Property(e => e.UpdatedAt).CurrentValue = null;
                    entry.Property(e => e.IsDeleted).CurrentValue = false;
                    break;

                case EntityState.Modified:
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.IsDeleted).CurrentValue = true;
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    break;

                case EntityState.Detached:
                case EntityState.Unchanged:
                default:
                    break;
            }
        }
    }

    /// <summary>Key generation and global soft-delete query filter for every root <see cref="BaseEntity"/>.</summary>
    private static void ApplyBaseEntityConventions(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes().ToList())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || entityType.BaseType is not null)
            {
                continue;
            }

            var entity = builder.Entity(entityType.ClrType);
            entity.Property(nameof(BaseEntity.Id)).ValueGeneratedNever();
            entity.HasIndex(nameof(BaseEntity.IsDeleted));

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var notDeleted = Expression.Not(Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));
            entity.HasQueryFilter(Expression.Lambda(notDeleted, parameter));
        }
    }
}
