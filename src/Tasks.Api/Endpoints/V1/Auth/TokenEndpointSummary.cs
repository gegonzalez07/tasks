using FastEndpoints;
using Tasks.Application.Auth.Dtos;

namespace Tasks.Api.Endpoints.V1.Auth;

public sealed class TokenEndpointSummary : Summary<TokenEndpoint>
{
    public TokenEndpointSummary()
    {
        Summary = "Obtain an access token";
        Description =
            "Validates the user credentials (user name or e-mail plus password) against ASP.NET Core Identity and " +
            "returns a JWT signed by the embedded Duende IdentityServer. Send it in subsequent requests as " +
            "'Authorization: Bearer {accessToken}'. Repeated failures lock the account temporarily.";
        ExampleRequest = new TokenRequest { UserName = "admin", Password = "Admin123!" };

        Response<TokenResponse>(200, "Token issued.");
        ResponseExamples[200] = new TokenResponse(
            "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
            "Bearer",
            3600,
            new DateTimeOffset(2026, 1, 1, 13, 0, 0, TimeSpan.Zero));
        Response<HttpValidationProblemDetails>(400, "The request is not valid (missing user name or password).");
        Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(401, "Invalid credentials, inactive or locked-out user.");
        Response<Microsoft.AspNetCore.Mvc.ProblemDetails>(500, "Unexpected server error.");
    }
}
