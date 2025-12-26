using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages;

public partial class LoginWithRecoveryCode(
    SignInManager<CalcioUserEntity> signInManager,
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager,
    ILogger<LoginWithRecoveryCode> logger)
{
    private string? message;
    private CalcioUserEntity user = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        // Ensure the user has gone through the username & password screen first
        user = await signInManager.GetTwoFactorAuthenticationUserAsync() ??
            throw new InvalidOperationException("Unable to load two-factor authentication user.");
    }

    private async Task OnValidSubmitAsync()
    {
        var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

        var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        var userId = await userManager.GetUserIdAsync(user);

        if (result.Succeeded)
        {
            LogUserLoggedInWithRecoveryCode(logger, userId);
            redirectManager.RedirectTo(ReturnUrl);
        }
        else if (result.IsLockedOut)
        {
            LogUserAccountLockedOut(logger);
            redirectManager.RedirectTo("Account/Lockout");
        }
        else
        {
            LogInvalidRecoveryCode(logger, userId);
            message = "Error: Invalid recovery code entered.";
        }
    }

    private sealed class InputModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; } = "";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' logged in with a recovery code.")]
    private static partial void LogUserLoggedInWithRecoveryCode(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User account locked out.")]
    private static partial void LogUserAccountLockedOut(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid recovery code entered for user with ID '{UserId}' ")]
    private static partial void LogInvalidRecoveryCode(ILogger logger, string userId);
}
