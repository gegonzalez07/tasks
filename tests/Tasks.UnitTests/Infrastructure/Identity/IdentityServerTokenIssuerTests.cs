using System.Security.Claims;
using AutoFixture;
using Duende.IdentityServer;
using Moq;
using Tasks.Application.Auth.Abstractions;
using Tasks.Infrastructure.Identity;
using Tasks.Infrastructure.Options;

namespace Tasks.UnitTests.Infrastructure.Identity;

public sealed class IdentityServerTokenIssuerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Fixture _fixture = new();
    private readonly Mock<IIdentityServerTools> _tools = new();
    private readonly Mock<TimeProvider> _clock = new();
    private readonly AuthenticationSettings _settings = new()
    {
        Audience = "tasks-api",
        Scope = "tasks.api",
        AccessTokenLifetimeSeconds = 900,
    };

    private List<Claim> _capturedClaims = [];
    private int _capturedLifetime;

    public IdentityServerTokenIssuerTests()
    {
        _clock.Setup(c => c.GetUtcNow()).Returns(Now);
        _tools
            .Setup(t => t.IssueJwtAsync(It.IsAny<int>(), It.IsAny<IEnumerable<Claim>>()))
            .Callback<int, IEnumerable<Claim>>((lifetime, claims) =>
            {
                _capturedLifetime = lifetime;
                _capturedClaims = claims.ToList();
            })
            .ReturnsAsync("signed.jwt.token");
    }

    [Fact]
    public async Task IssueAsync_IncludesIdentityRolesAudienceAndScope()
    {
        var user = new AuthenticatedUser(Guid.CreateVersion7(), _fixture.Create<string>(), "admin@tasks.local", ["Admin", "Auditor"]);

        var token = await CreateSut().IssueAsync(user);

        Assert.Equal("signed.jwt.token", token.AccessToken);
        Assert.Equal(900, _capturedLifetime);
        Assert.Equal(900, token.ExpiresIn);
        Assert.Equal(Now.AddSeconds(900), token.ExpiresAt);
        Assert.Contains(_capturedClaims, c => c.Type == "sub" && c.Value == user.Id.ToString());
        Assert.Contains(_capturedClaims, c => c.Type == "name" && c.Value == user.UserName);
        Assert.Contains(_capturedClaims, c => c.Type == "email" && c.Value == user.Email);
        Assert.Contains(_capturedClaims, c => c.Type == "aud" && c.Value == "tasks-api");
        Assert.Contains(_capturedClaims, c => c.Type == "scope" && c.Value == "tasks.api");
        Assert.Equal(new[] { "Admin", "Auditor" }, _capturedClaims.Where(c => c.Type == "role").Select(c => c.Value));
    }

    [Fact]
    public async Task IssueAsync_WithoutEmail_OmitsEmailClaim()
    {
        var user = new AuthenticatedUser(Guid.CreateVersion7(), _fixture.Create<string>(), null, []);

        await CreateSut().IssueAsync(user);

        Assert.DoesNotContain(_capturedClaims, c => c.Type == "email");
        Assert.DoesNotContain(_capturedClaims, c => c.Type == "role");
    }

    private IdentityServerTokenIssuer CreateSut() =>
        new(_tools.Object, Microsoft.Extensions.Options.Options.Create(_settings), _clock.Object);
}
