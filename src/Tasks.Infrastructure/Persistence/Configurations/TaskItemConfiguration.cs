using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tasks.Domain.TaskItems;

namespace Tasks.Infrastructure.Persistence.Configurations;

/// <summary>
/// Table <c>Tasks</c>. Key, soft-delete filter and IsDeleted index come from the
/// <see cref="Domain.Common.BaseEntity"/> conventions in <see cref="AppDbContext"/>.
/// </summary>
internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.Property(t => t.Title)
            .HasMaxLength(TaskItem.TitleMaxLength)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(TaskItem.DescriptionMaxLength);

        builder.Property(t => t.Assignee)
            .HasMaxLength(TaskItem.AssigneeMaxLength)
            .IsRequired();

        // Stored as text ("Pending", "InProgress", "Done"): readable in the database and safe if the enum is reordered.
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength)
            .IsRequired();

        builder.Property(t => t.DueDate)
            .IsRequired();

        // Support the list filters (status / assignee) and the default ordering (due date).
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Assignee);
        builder.HasIndex(t => t.DueDate);
    }
}
