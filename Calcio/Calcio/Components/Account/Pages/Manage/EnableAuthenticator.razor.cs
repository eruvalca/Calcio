using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;

using Calcio.Data;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class EnableAuthenticator(
    UserManager<ApplicationUser> userManager,
    UrlEncoder urlEncoder,
    IdentityRedirectManager redirectManager,
    ILogger<EnableAuthenticator> logger)
{
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    private string? message;
    private ApplicationUser? user;
    private string? sharedKey;
    private string? authenticatorUri;
    private IEnumerable<string>? recoveryCodes;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

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

    private async ValueTask LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
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

    private string GenerateQrCodeUri(string email, string unformattedKey) => string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            urlEncoder.Encode("Microsoft.AspNetCore.Identity.UI"),
            urlEncoder.Encode(email),
            unformattedKey);

    private sealed class InputModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = "";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' has enabled 2FA with an authenticator app.")]
    private static partial void LogUserEnabled2fa(ILogger logger, string userId);
}
