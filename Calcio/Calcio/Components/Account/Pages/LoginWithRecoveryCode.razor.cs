using System.ComponentModel.DataAnnotations;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the Login With Recovery Code.
/// </summary>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="userManager">The user Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
/// <param name="logger">The logger.</param>
public partial class LoginWithRecoveryCode(
    SignInManager<CalcioUserEntity> signInManager,
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager,
    ILogger<LoginWithRecoveryCode> logger)
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

    /// <summary>
    /// Represents the Input Model.
    /// </summary>
    private sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the Recovery Code.
        /// </summary>
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery Code")]
        /// <summary>
        /// Gets or sets the recovery code.
        /// </summary>
        public string RecoveryCode { get; set; } = "";
    }

    /// <summary>
    /// Executes the Log User Logged In With Recovery Code operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' logged in with a recovery code.")]
    /// <summary>
    /// Executes the log user logged in with recovery code operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogUserLoggedInWithRecoveryCode(ILogger logger, string userId);

    /// <summary>
    /// Executes the Log User Account Locked Out operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "User account locked out.")]
    /// <summary>
    /// Executes the log user account locked out operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    private static partial void LogUserAccountLockedOut(ILogger logger);

    /// <summary>
    /// Executes the Log Invalid Recovery Code operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid recovery code entered for user with ID '{UserId}' ")]
    /// <summary>
    /// Executes the log invalid recovery code operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogInvalidRecoveryCode(ILogger logger, string userId);
}
