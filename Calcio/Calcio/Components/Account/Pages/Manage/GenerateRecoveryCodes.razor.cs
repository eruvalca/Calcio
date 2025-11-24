using Calcio.Shared.Models.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class GenerateRecoveryCodes(
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager,
    ILogger<GenerateRecoveryCodes> logger)
{
    private string? message;
    private CalcioUserEntity? user;
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

        LogUserGeneratedRecoveryCodes(logger, userId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' has generated new 2FA recovery codes.")]
    private static partial void LogUserGeneratedRecoveryCodes(ILogger logger, string userId);
}
