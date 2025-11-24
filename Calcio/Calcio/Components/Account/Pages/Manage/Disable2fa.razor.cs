using Calcio.Shared.Models.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class Disable2fa(
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager,
    ILogger<Disable2fa> logger)
{
    private CalcioUserEntity? user;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

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

    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' has disabled 2fa.")]
    private static partial void LogUserDisabled2fa(ILogger logger, string userId);
}
