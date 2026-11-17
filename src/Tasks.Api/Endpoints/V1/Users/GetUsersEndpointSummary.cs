using FastEndpoints;
using Tasks.Application.Common.Pagination;
using Tasks.Application.Users;

namespace Tasks.Api.Endpoints.V1.Users;

public sealed class GetUsersEndpointSummary : Summary<GetUsersEndpoint>
{
    public GetUsersEndpointSummary()
    {
        Summary = "List users (paged)";
        Description =
            "Returns users ordered by user name using the standard pagination contract " +
            "(pageNumber starting at 1, pageSize between 1 and 100). Optional 'search' filters by user name or e-mail. " +
            "Requires the Admin role.";
        Params[nameof(GetUsersRequest.PageNumber)] = "1-based page number (default 1).";
        Params[nameof(GetUsersRequest.PageSize)] = $"Items per page, 1-{PagedRequest.MaxPageSize} (default {PagedRequest.DefaultPageSize}).";
        Params[nameof(GetUsersRequest.Search)] = "Optional text contained in the user name or e-mail.";

        Response<PagedResponse<UserDto>>(200, "A page of users.");
        Response<HttpValidationProblemDetails>(400, "Invalid pagination parameters.");
        Response(401, "Missing or invalid bearer token.");
        Response(403, "The caller does not have the Admin role.");
    }
}
