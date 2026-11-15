using FastEndpoints;
using Tasks.Api.Configuration;
using Tasks.Application.Auth;
using Tasks.Application.Auth.Dtos;

namespace Tasks.Api.Endpoints.V1.Auth;

/// <summary>POST /api/v1/auth/token — exchanges credentials for a bearer token.</summary>
public sealed class TokenEndpoint(IAuthService authService) : Endpoint<TokenRequest, TokenResponse>
{
    public override void Configure()
    {
        Post("auth/token");
        Version(ApiVersions.V1);
        AllowAnonymous();
        Description(builder => builder.WithTags("Authentication"));
    }

    public override async Task HandleAsync(TokenRequest req, CancellationToken ct)
    {
        var response = await authService.AuthenticateAsync(req, ct);
        await Send.OkAsync(response, ct);
    }
}
