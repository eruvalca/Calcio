using System.ComponentModel.DataAnnotations;

using Calcio.Data.Models.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages;

public partial class LoginWith2fa(
    SignInManager<CalcioUserEntity> signInManager,
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager,
    ILogger<LoginWith2fa> logger)
{
    private string? message;
    private CalcioUserEntity user = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery]
    private bool RememberMe { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        // Ensure the user has gone through the username & password screen first
        user = await signInManager.GetTwoFactorAuthenticationUserAsync() ??
            throw new InvalidOperationException("Unable to load two-factor authentication user.");
    }

    private async Task OnValidSubmitAsync()
    {
        var authenticatorCode = Input.TwoFactorCode!.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, RememberMe, Input.RememberMachine);
        var userId = await userManager.GetUserIdAsync(user);

        if (result.Succeeded)
        {
            LogUserLoggedInWith2fa(logger, userId);
            redirectManager.RedirectTo(ReturnUrl);
        }
        else if (result.IsLockedOut)
        {
            LogUserAccountLockedOut(logger, userId);
            redirectManager.RedirectTo("Account/Lockout");
        }
        else
        {
            LogInvalidAuthenticatorCode(logger, userId);
            message = "Error: Invalid authenticator code.";
        }
    }

    private sealed class InputModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator code")]
        public string? TwoFactorCode { get; set; }

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' logged in with 2fa.")]
    private static partial void LogUserLoggedInWith2fa(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User with ID '{UserId}' account locked out.")]
    private static partial void LogUserAccountLockedOut(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid authenticator code entered for user with ID '{UserId}'.")]
    private static partial void LogInvalidAuthenticatorCode(ILogger logger, string userId);
}
