using FastEndpoints;
using Tasks.Application.TaskItems;

namespace Tasks.Api.Endpoints.V1.TaskItems;

public sealed class GetTaskEndpointSummary : Summary<GetTaskEndpoint>
{
    public GetTaskEndpointSummary()
    {
        Summary = "Get a task";
        Description = "Returns the task with the given id. Deleted tasks are reported as not found.";
        Params["id"] = "Task id (GUID). A value that is not a GUID does not match the route (404).";

        Response<TaskDto>(200, "The task.");
        Response(401, "Missing or invalid bearer token.");
        Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(404, "The task does not exist or was deleted.");
    }
}
