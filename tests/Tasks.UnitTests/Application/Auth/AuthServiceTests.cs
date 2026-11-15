using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Moq;
using Tasks.Application.Auth;
using Tasks.Application.Auth.Abstractions;
using Tasks.Application.Auth.Dtos;
using Tasks.Application.Common.Exceptions;
using Tasks.UnitTests.Common;

namespace Tasks.UnitTests.Application.Auth;

public sealed class AuthServiceTests
{
    [Theory]
    [AutoMoqData]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsBearerToken(
        [Frozen] Mock<IIdentityService> identityService,
        [Frozen] Mock<ITokenIssuer> tokenIssuer,
        TokenRequest request,
        AuthenticatedUser user,
        IssuedToken issuedToken,
        AuthService sut)
    {
        identityService
            .Setup(s => s.ValidateCredentialsAsync(request.UserName, request.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        tokenIssuer
            .Setup(i => i.IssueAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(issuedToken);

        var response = await sut.AuthenticateAsync(request);

        Assert.Equal(issuedToken.AccessToken, response.AccessToken);
        Assert.Equal(AuthService.BearerTokenType, response.TokenType);
        Assert.Equal(issuedToken.ExpiresIn, response.ExpiresIn);
        Assert.Equal(issuedToken.ExpiresAt, response.ExpiresAt);
        tokenIssuer.Verify(i => i.IssueAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoMoqData]
    public async Task AuthenticateAsync_InvalidCredentials_ThrowsAndDoesNotIssueToken(
        [Frozen] Mock<IIdentityService> identityService,
        [Frozen] Mock<ITokenIssuer> tokenIssuer,
        TokenRequest request,
        AuthService sut)
    {
        identityService
            .Setup(s => s.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticatedUser?)null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => sut.AuthenticateAsync(request));

        tokenIssuer.Verify(i => i.IssueAsync(It.IsAny<AuthenticatedUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "secret")]
    [InlineData("   ", "secret")]
    [InlineData("admin", "")]
    public async Task AuthenticateAsync_MissingCredentials_ThrowsWithoutQueryingTheStore(string userName, string password)
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
        var identityService = fixture.Freeze<Mock<IIdentityService>>();
        var sut = fixture.Create<AuthService>();

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => sut.AuthenticateAsync(new TokenRequest { UserName = userName, Password = password }));

        identityService.Verify(
            s => s.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [AutoMoqData]
    public async Task AuthenticateAsync_TrimsUserNameAndPropagatesCancellationToken(
        [Frozen] Mock<IIdentityService> identityService,
        [Frozen] Mock<ITokenIssuer> tokenIssuer,
        AuthenticatedUser user,
        IssuedToken issuedToken,
        AuthService sut)
    {
        using var cts = new CancellationTokenSource();
        var request = new TokenRequest { UserName = "  admin  ", Password = "P@ssw0rd!" };
        identityService
            .Setup(s => s.ValidateCredentialsAsync("admin", request.Password, cts.Token))
            .ReturnsAsync(user);
        tokenIssuer
            .Setup(i => i.IssueAsync(user, cts.Token))
            .ReturnsAsync(issuedToken);

        var response = await sut.AuthenticateAsync(request, cts.Token);

        Assert.Equal(issuedToken.AccessToken, response.AccessToken);
        identityService.Verify(s => s.ValidateCredentialsAsync("admin", request.Password, cts.Token), Times.Once);
    }

    [Theory]
    [AutoMoqData]
    public async Task AuthenticateAsync_NullRequest_Throws(AuthService sut)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.AuthenticateAsync(null!));
    }
}
