using System.Security.Claims;

using Calcio.Entities;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Calcio.Components.Account;

// This is a server-side AuthenticationStateProvider that revalidates the security stamp for the connected user
// every 30 minutes an interactive circuit is connected.
/// <summary>
/// Represents the Identity Revalidating Authentication State Provider.
/// </summary>
/// <param name="loggerFactory">The logger Factory.</param>
/// <param name="scopeFactory">The scope Factory.</param>
/// <param name="loggerFactory">The logger Factory.</param>
internal sealed class IdentityRevalidatingAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<IdentityOptions> options)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    /// <summary>
    /// Gets the Revalidation Interval.
    /// </summary>
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    /// <summary>
    /// Executes the Validate Authentication State Async operation.
    /// </summary>
    /// <param name="authenticationState">The authentication State.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        // Get the user manager from a new scope to ensure it fetches fresh data
        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CalcioUserEntity>>();
        return await ValidateSecurityStampAsync(userManager, authenticationState.User);
    }

    /// <summary>
    /// Executes the Validate Security Stamp Async operation.
    /// </summary>
    /// <param name="userManager">The user Manager.</param>
    /// <param name="principal">The principal.</param>
    /// <returns>The operation result.</returns>
    private async Task<bool> ValidateSecurityStampAsync(UserManager<CalcioUserEntity> userManager, ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return false;
        }
        else if (!userManager.SupportsUserSecurityStamp)
        {
            return true;
        }
        else
        {
            var principalStamp = principal.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType);
            var userStamp = await userManager.GetSecurityStampAsync(user);
            return principalStamp == userStamp;
        }
    }
}
