using Tasks.Application.Common.Pagination;

namespace Tasks.Application.Users;

public interface IUserQueryService
{
    Task<PagedResponse<UserDto>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default);
}
