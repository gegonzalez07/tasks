using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tasks.Api.Configuration;
using Tasks.Application.Common.Pagination;
using Tasks.Application.Common.Security;
using Tasks.Application.Users;

namespace Tasks.Api.Endpoints.V1.Users;

/// <summary>GET /api/v1/users — paged list of users (Admin only).</summary>
public sealed class GetUsersEndpoint(IUserQueryService userQueryService) : Endpoint<GetUsersRequest, PagedResponse<UserDto>>
{
    public override void Configure()
    {
        Get("users");
        Version(ApiVersions.V1);
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Roles(AppRoles.Admin);
        Description(builder => builder.WithTags("Users"));
    }

    public override async Task HandleAsync(GetUsersRequest req, CancellationToken ct)
    {
        var page = await userQueryService.GetUsersAsync(req, ct);
        await Send.OkAsync(page, ct);
    }
}
