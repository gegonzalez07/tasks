using FastEndpoints;

namespace Tasks.Api.Endpoints.V1.TaskItems;

public sealed class DeleteTaskEndpointSummary : Summary<DeleteTaskEndpoint>
{
    public DeleteTaskEndpointSummary()
    {
        Summary = "Delete a task";
        Description = "Soft-deletes the task: it disappears from the API but the row is kept for auditing.";
        Params["id"] = "Task id (GUID).";

        Response(204, "Task deleted.");
        Response(401, "Missing or invalid bearer token.");
        Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(404, "The task does not exist or was already deleted.");
    }
}
