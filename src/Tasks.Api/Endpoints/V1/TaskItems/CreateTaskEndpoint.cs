using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tasks.Api.Configuration;
using Tasks.Application.TaskItems;

namespace Tasks.Api.Endpoints.V1.TaskItems;

/// <summary>POST /api/v1/tasks — creates a task and returns it with a Location header.</summary>
public sealed class CreateTaskEndpoint(ITaskService taskService) : Endpoint<CreateTaskRequest, TaskDto>
{
    public override void Configure()
    {
        Post("tasks");
        Version(ApiVersions.V1);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Description(builder => builder.WithTags(TaskEndpointsTag.Name));
    }

    public override async Task HandleAsync(CreateTaskRequest req, CancellationToken ct)
    {
        var task = await taskService.CreateAsync(req, ct);

        HttpContext.Response.Headers.Location = $"{HttpContext.Request.PathBase}{HttpContext.Request.Path}/{task.Id}";
        await Send.ResponseAsync(task, StatusCodes.Status201Created, ct);
    }
}
