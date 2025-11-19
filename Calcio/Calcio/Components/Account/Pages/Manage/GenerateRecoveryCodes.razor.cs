using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

using Calcio.Data;

namespace Calcio.Components.Account.Pages.Manage;

public partial class GenerateRecoveryCodes(
    UserManager<ApplicationUser> userManager,
    IdentityRedirectManager redirectManager,
    ILogger<GenerateRecoveryCodes> logger)
{
    private string? message;
    private ApplicationUser? user;
    private IEnumerable<string>? recoveryCodes;

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

        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        if (!isTwoFactorEnabled)
        {
            throw new InvalidOperationException("Cannot generate recovery codes for user because they do not have 2FA enabled.");
        }
    }

    private async Task OnSubmitAsync()
    {
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        var userId = await userManager.GetUserIdAsync(user);
        recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        message = "You have generated new recovery codes.";

        logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
    }
}
