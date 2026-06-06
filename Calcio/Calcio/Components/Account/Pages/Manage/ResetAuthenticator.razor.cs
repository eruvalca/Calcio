using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the Reset Authenticator.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
/// <param name="logger">The logger.</param>
public partial class ResetAuthenticator(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager,
    ILogger<ResetAuthenticator> logger)
{
    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Executes the On Submit Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task OnSubmitAsync()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        var userId = await userManager.GetUserIdAsync(user);
        LogUserResetAuthenticatorKey(logger, userId);

        await signInManager.RefreshSignInAsync(user);

        redirectManager.RedirectToWithStatus(
            "Account/Manage/EnableAuthenticator",
            "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.",
            HttpContext);
    }

    /// <summary>
    /// Executes the Log User Reset Authenticator Key operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' has reset their authentication app key.")]
    /// <summary>
    /// Executes the log user reset authenticator key operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogUserResetAuthenticatorKey(ILogger logger, string userId);
}
