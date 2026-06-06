using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the Disable2fa.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
/// <param name="logger">The logger.</param>
public partial class Disable2fa(
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager,
    ILogger<Disable2fa> logger)
{
    /// <summary>
    /// Stores the user.
    /// </summary>
    private CalcioUserEntity? user;

    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Executes the On Initialized Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    protected override async Task OnInitializedAsync()
    {
        user = await userManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        if (HttpMethods.IsGet(HttpContext.Request.Method) && !await userManager.GetTwoFactorEnabledAsync(user))
        {
            throw new InvalidOperationException("Cannot disable 2FA for user as it's not currently enabled.");
        }
    }

    /// <summary>
    /// Executes the On Submit Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task OnSubmitAsync()
    {
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        var disable2faResult = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disable2faResult.Succeeded)
        {
            throw new InvalidOperationException("Unexpected error occurred disabling 2FA.");
        }

        var userId = await userManager.GetUserIdAsync(user);
        LogUserDisabled2fa(logger, userId);

        redirectManager.RedirectToWithStatus(
            "Account/Manage/TwoFactorAuthentication",
            "2fa has been disabled. You can reenable 2fa when you setup an authenticator app",
            HttpContext);
    }

    /// <summary>
    /// Executes the Log User Disabled2fa operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' has disabled 2fa.")]
    /// <summary>
    /// Executes the log user disabled2fa operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogUserDisabled2fa(ILogger logger, string userId);
}
