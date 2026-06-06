using System.ComponentModel.DataAnnotations;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the Login With2fa.
/// </summary>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="userManager">The user Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
/// <param name="logger">The logger.</param>
public partial class LoginWith2fa(
    SignInManager<CalcioUserEntity> signInManager,
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager,
    ILogger<LoginWith2fa> logger)
{
    /// <summary>
    /// Stores the message.
    /// </summary>
    private string? message;
    /// <summary>
    /// Stores the user.
    /// </summary>
    private CalcioUserEntity user = default!;

    /// <summary>
    /// Gets or sets the Input.
    /// </summary>
    [SupplyParameterFromForm]
    /// <summary>
    /// Gets or sets the input.
    /// </summary>
    private InputModel Input { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Return Url.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the return url.
    /// </summary>
    private string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the Remember Me.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the remember me.
    /// </summary>
    private bool RememberMe { get; set; }

    /// <summary>
    /// Executes the On Initialized Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        // Ensure the user has gone through the username & password screen first
        user = await signInManager.GetTwoFactorAuthenticationUserAsync() ??
            throw new InvalidOperationException("Unable to load two-factor authentication user.");
    }

    /// <summary>
    /// Executes the On Valid Submit Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Represents the Input Model.
    /// </summary>
    private sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the Two Factor Code.
        /// </summary>
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator code")]
        /// <summary>
        /// Gets or sets the two factor code.
        /// </summary>
        public string? TwoFactorCode { get; set; }

        /// <summary>
        /// Gets or sets the Remember Machine.
        /// </summary>
        [Display(Name = "Remember this machine")]
        /// <summary>
        /// Gets or sets the remember machine.
        /// </summary>
        public bool RememberMachine { get; set; }
    }

    /// <summary>
    /// Executes the Log User Logged In With2fa operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' logged in with 2fa.")]
    /// <summary>
    /// Executes the log user logged in with2fa operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogUserLoggedInWith2fa(ILogger logger, string userId);

    /// <summary>
    /// Executes the Log User Account Locked Out operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "User with ID '{UserId}' account locked out.")]
    /// <summary>
    /// Executes the log user account locked out operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogUserAccountLockedOut(ILogger logger, string userId);

    /// <summary>
    /// Executes the Log Invalid Authenticator Code operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid authenticator code entered for user with ID '{UserId}'.")]
    /// <summary>
    /// Executes the log invalid authenticator code operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogInvalidAuthenticatorCode(ILogger logger, string userId);
}
