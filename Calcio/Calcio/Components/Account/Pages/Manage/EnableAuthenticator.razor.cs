using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the Enable Authenticator.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="urlEncoder">The url Encoder.</param>
/// <param name="redirectManager">The redirect Manager.</param>
/// <param name="logger">The logger.</param>
public partial class EnableAuthenticator(
    UserManager<CalcioUserEntity> userManager,
    UrlEncoder urlEncoder,
    IdentityRedirectManager redirectManager,
    ILogger<EnableAuthenticator> logger)
{
    /// <summary>
    /// Stores the Authenticator Uri Format.
    /// </summary>
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    /// <summary>
    /// Stores the user.
    /// </summary>
    private string? message;
    /// <summary>
    /// Stores the shared Key.
    /// </summary>
    private CalcioUserEntity? user;
    /// <summary>
    /// Stores the authenticator Uri.
    /// </summary>
    private string? sharedKey;
    /// <summary>
    /// Stores the recovery Codes.
    /// </summary>
    private string? authenticatorUri;
    /// <summary>
    /// Stores the recovery Codes.
    /// </summary>
    private IEnumerable<string>? recoveryCodes;

    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Input.
    /// </summary>
    [SupplyParameterFromForm]
    /// <summary>
    /// Gets or sets the input.
    /// </summary>
    private InputModel Input { get; set; } = default!;

    /// <summary>
    /// Executes the On Initialized Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        user = await userManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        await LoadSharedKeyAndQrCodeUriAsync(user);
    }

    /// <summary>
    /// Executes the On Valid Submit Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task OnValidSubmitAsync()
    {
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        // Strip spaces and hyphens
        var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var is2faTokenValid = await userManager.VerifyTwoFactorTokenAsync(
            user, userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!is2faTokenValid)
        {
            message = "Error: Verification code is invalid.";
            return;
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var userId = await userManager.GetUserIdAsync(user);
        LogUserEnabled2fa(logger, userId);

        message = "Your authenticator app has been verified.";

        if (await userManager.CountRecoveryCodesAsync(user) == 0)
        {
            recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        }
        else
        {
            redirectManager.RedirectToWithStatus("Account/Manage/TwoFactorAuthentication", message, HttpContext);
        }
    }

    /// <summary>
    /// Executes the Load Shared Key And Qr Code Uri Async operation.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The operation result.</returns>
    private async ValueTask LoadSharedKeyAndQrCodeUriAsync(CalcioUserEntity user)
    {
        // Load the authenticator key & QR code URI to display on the form
        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        }

        sharedKey = FormatKey(unformattedKey!);

        var email = await userManager.GetEmailAsync(user);
        authenticatorUri = GenerateQrCodeUri(email!, unformattedKey!);
    }

    /// <summary>
    /// Executes the Format Key operation.
    /// </summary>
    /// <param name="unformattedKey">The unformatted Key.</param>
    /// <returns>The operation result.</returns>
    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Executes the Generate Qr Code Uri operation.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <param name="unformattedKey">The unformatted Key.</param>
    /// <returns>The operation result.</returns>
    private string GenerateQrCodeUri(string email, string unformattedKey) => string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            urlEncoder.Encode("Microsoft.AspNetCore.Identity.UI"),
            urlEncoder.Encode(email),
            unformattedKey);

    /// <summary>
    /// Represents the Input Model.
    /// </summary>
    private sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the Code.
        /// </summary>
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Verification Code")]
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        public string Code { get; set; } = "";
    }

    /// <summary>
    /// Executes the Log User Enabled2fa operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' has enabled 2FA with an authenticator app.")]
    /// <summary>
    /// Executes the log user enabled2fa operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogUserEnabled2fa(ILogger logger, string userId);
}
