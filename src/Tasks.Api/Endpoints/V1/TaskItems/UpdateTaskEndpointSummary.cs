using FastEndpoints;
using Tasks.Application.TaskItems;
using Tasks.Domain.TaskItems;

namespace Tasks.Api.Endpoints.V1.TaskItems;

public sealed class UpdateTaskEndpointSummary : Summary<UpdateTaskEndpoint>
{
    public UpdateTaskEndpointSummary()
    {
        Summary = "Update a task";
        Description =
            "Replaces title, description, status, assignee and due date (PUT semantics: send every field). " +
            "An overdue task can be edited while keeping its due date, but the due date cannot be moved into the past.";
        Params["id"] = "Task id (GUID).";
        ExampleRequest = new UpdateTaskRequest
        {
            Title = "Write the README",
            Description = "Add the AI usage section.",
            Assignee = "luis",
            Status = TaskItemStatus.InProgress,
            DueDate = new DateOnly(2026, 12, 5),
        };

        Response<TaskDto>(200, "The updated task.");
        Response<FastEndpoints.ProblemDetails>(400, "Invalid data, or due date moved into the past.");
        Response(401, "Missing or invalid bearer token.");
        Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(404, "The task does not exist or was deleted.");
    }
}
