using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tasks.Api.Configuration;
using Tasks.Application.Common.Pagination;
using Tasks.Application.TaskItems;

namespace Tasks.Api.Endpoints.V1.TaskItems;

/// <summary>GET /api/v1/tasks — paged list with optional status / assignee filters.</summary>
public sealed class GetTasksEndpoint(ITaskService taskService) : Endpoint<GetTasksRequest, PagedResponse<TaskDto>>
{
    public override void Configure()
    {
        Get("tasks");
        Version(ApiVersions.V1);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Description(builder => builder.WithTags(TaskEndpointsTag.Name));
    }

    public override async Task HandleAsync(GetTasksRequest req, CancellationToken ct)
    {
        var page = await taskService.GetPagedAsync(req, ct);
        await Send.OkAsync(page, ct);
    }
}
