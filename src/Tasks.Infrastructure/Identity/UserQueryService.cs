using Microsoft.EntityFrameworkCore;
using Tasks.Application.Common.Pagination;
using Tasks.Application.Users;
using Tasks.Infrastructure.Persistence;

namespace Tasks.Infrastructure.Identity;

internal sealed class UserQueryService(AppDbContext context) : IUserQueryService
{
    public Task<PagedResponse<UserDto>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(u => u.UserName!.Contains(search) || (u.Email != null && u.Email.Contains(search)));
        }

        return query
            .OrderBy(u => u.UserName)
            .Select(u => new UserDto(u.Id, u.UserName!, u.Email, u.FullName, u.IsActive, u.CreatedAt))
            .ToPagedResponseAsync(request, cancellationToken);
    }
}
