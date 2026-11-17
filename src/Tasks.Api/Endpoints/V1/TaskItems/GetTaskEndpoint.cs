using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tasks.Api.Configuration;
using Tasks.Application.TaskItems;

namespace Tasks.Api.Endpoints.V1.TaskItems;

/// <summary>GET /api/v1/tasks/{id} — a single task.</summary>
public sealed class GetTaskEndpoint(ITaskService taskService) : EndpointWithoutRequest<TaskDto>
{
    public override void Configure()
    {
        Get("tasks/{id:guid}");
        Version(ApiVersions.V1);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Description(builder => builder.WithTags(TaskEndpointsTag.Name));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var task = await taskService.GetByIdAsync(Route<Guid>("id"), ct);
        await Send.OkAsync(task, ct);
    }
}
