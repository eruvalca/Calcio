using Calcio.Shared.Models.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class ResetAuthenticator(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager,
    ILogger<ResetAuthenticator> logger)
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

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

    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' has reset their authentication app key.")]
    private static partial void LogUserResetAuthenticatorKey(ILogger logger, string userId);
}
