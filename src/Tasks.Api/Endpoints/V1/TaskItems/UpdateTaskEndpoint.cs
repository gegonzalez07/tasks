using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tasks.Api.Configuration;
using Tasks.Application.TaskItems;

namespace Tasks.Api.Endpoints.V1.TaskItems;

/// <summary>PUT /api/v1/tasks/{id} — replaces every editable field of a task.</summary>
public sealed class UpdateTaskEndpoint(ITaskService taskService) : Endpoint<UpdateTaskRequest, TaskDto>
{
    public override void Configure()
    {
        Put("tasks/{id:guid}");
        Version(ApiVersions.V1);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Description(builder => builder.WithTags(TaskEndpointsTag.Name));
    }

    public override async Task HandleAsync(UpdateTaskRequest req, CancellationToken ct)
    {
        // The id always comes from the route, never from the body.
        req.Id = Route<Guid>("id");

        var task = await taskService.UpdateAsync(req, ct);
        await Send.OkAsync(task, ct);
    }
}
