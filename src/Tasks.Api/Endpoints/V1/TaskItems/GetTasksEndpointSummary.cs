using FastEndpoints;
using Tasks.Application.Common.Pagination;
using Tasks.Application.TaskItems;

namespace Tasks.Api.Endpoints.V1.TaskItems;

public sealed class GetTasksEndpointSummary : Summary<GetTasksEndpoint>
{
    public GetTasksEndpointSummary()
    {
        Summary = "List tasks (filtered and paged)";
        Description =
            "Returns tasks ordered by due date (soonest first). Filters are optional and combined with AND. " +
            "Deleted tasks are never returned.";
        Params[nameof(GetTasksRequest.Status)] = "Optional status filter: Pending, InProgress or Done.";
        Params[nameof(GetTasksRequest.Assignee)] = "Optional assignee filter (exact match, case-insensitive).";
        Params[nameof(GetTasksRequest.PageNumber)] = "1-based page number (default 1).";
        Params[nameof(GetTasksRequest.PageSize)] = $"Items per page, 1-{PagedRequest.MaxPageSize} (default {PagedRequest.DefaultPageSize}).";

        Response<PagedResponse<TaskDto>>(200, "A page of tasks.");
        Response<FastEndpoints.ProblemDetails>(400, "Invalid filter or pagination values.");
        Response(401, "Missing or invalid bearer token.");
    }
}
