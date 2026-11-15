using Microsoft.AspNetCore.Identity;
using Tasks.Application.Auth.Abstractions;

namespace Tasks.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity adapter for <see cref="IIdentityService"/>. Honours lockout and the active flag.
/// </summary>
internal sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userNameOrEmail);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByNameAsync(userNameOrEmail).ConfigureAwait(false);
        if (user is null && userNameOrEmail.Contains('@', StringComparison.Ordinal))
        {
            user = await userManager.FindByEmailAsync(userNameOrEmail).ConfigureAwait(false);
        }

        if (user is null || !user.IsActive)
        {
            return null;
        }

        if (userManager.SupportsUserLockout && await userManager.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
        {
            if (userManager.SupportsUserLockout)
            {
                await userManager.AccessFailedAsync(user).ConfigureAwait(false);
            }

            return null;
        }

        if (userManager.SupportsUserLockout && user.AccessFailedCount > 0)
        {
            await userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        }

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        return new AuthenticatedUser(user.Id, user.UserName!, user.Email, roles.ToArray());
    }
}
