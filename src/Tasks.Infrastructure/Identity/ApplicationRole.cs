using Microsoft.AspNetCore.Identity;

namespace Tasks.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
    {
        Id = Guid.CreateVersion7();
    }

    public ApplicationRole(string roleName)
        : this()
    {
        Name = roleName;
    }
}
