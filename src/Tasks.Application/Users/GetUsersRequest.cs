using Tasks.Application.Common.Pagination;

namespace Tasks.Application.Users;

/// <summary>Paged user query with an optional free-text filter on user name / e-mail.</summary>
public sealed class GetUsersRequest : PagedRequest
{
    public string? Search { get; set; }
}
