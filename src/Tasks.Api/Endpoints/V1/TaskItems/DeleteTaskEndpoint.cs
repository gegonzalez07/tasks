using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tasks.Api.Configuration;
using Tasks.Application.TaskItems;

namespace Tasks.Api.Endpoints.V1.TaskItems;

/// <summary>DELETE /api/v1/tasks/{id} — soft-deletes a task.</summary>
public sealed class DeleteTaskEndpoint(ITaskService taskService) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("tasks/{id:guid}");
        Version(ApiVersions.V1);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Description(builder => builder.WithTags(TaskEndpointsTag.Name));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await taskService.DeleteAsync(Route<Guid>("id"), ct);
        await Send.NoContentAsync(ct);
    }
}
