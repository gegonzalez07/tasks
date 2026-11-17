using FastEndpoints;
using Tasks.Application.TaskItems;
using Tasks.Domain.TaskItems;

namespace Tasks.Api.Endpoints.V1.TaskItems;

public sealed class CreateTaskEndpointSummary : Summary<CreateTaskEndpoint>
{
    public CreateTaskEndpointSummary()
    {
        Summary = "Create a task";
        Description =
            "Creates a task. 'title', 'assignee' and 'dueDate' (yyyy-MM-dd, today or later) are required; " +
            "'status' is optional (Pending, InProgress or Done; defaults to Pending). " +
            "Returns the created task and its URL in the Location header. " +
            "'Today' is evaluated in the time zone sent in the optional X-Time-Zone header (IANA id such as " +
            "'America/Bogota'); without it, in UTC. An unknown time zone returns 400.";
        ExampleRequest = new CreateTaskRequest
        {
            Title = "Write the README",
            Description = "How to run it, decisions and AI usage.",
            Assignee = "ana",
            Status = TaskItemStatus.Pending,
            DueDate = new DateOnly(2026, 12, 1),
        };

        Response<TaskDto>(201, "Task created.");
        Response<HttpValidationProblemDetails>(400, "Invalid data (missing fields, too long values, unknown status, past due date).");
        Response(401, "Missing or invalid bearer token.");
    }
}
