using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the Forgot Password.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="emailSender">The email Sender.</param>
/// <param name="navigationManager">The navigation Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class ForgotPassword(
    UserManager<CalcioUserEntity> userManager,
    IEmailSender<CalcioUserEntity> emailSender,
    NavigationManager navigationManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Gets or sets the Input.
    /// </summary>
    [SupplyParameterFromForm]
    /// <summary>
    /// Gets or sets the input.
    /// </summary>
    private InputModel Input { get; set; } = default!;

    /// <summary>
    /// Executes the On Initialized operation.
    /// </summary>
    protected override void OnInitialized() => Input ??= new();

    /// <summary>
    /// Executes the On Valid Submit Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task OnValidSubmitAsync()
    {
        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null || !await userManager.IsEmailConfirmedAsync(user))
        {
            // Don't reveal that the user does not exist or is not confirmed
            redirectManager.RedirectTo("Account/ForgotPasswordConfirmation");
            return;
        }

        // For more information on how to enable account confirmation and password reset please
        // visit https://go.microsoft.com/fwlink/?LinkID=532713
        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = navigationManager.GetUriWithQueryParameters(
            navigationManager.ToAbsoluteUri("Account/ResetPassword").AbsoluteUri,
            new Dictionary<string, object?> { ["code"] = code });

        await emailSender.SendPasswordResetLinkAsync(user, Input.Email, HtmlEncoder.Default.Encode(callbackUrl));

        redirectManager.RedirectTo("Account/ForgotPasswordConfirmation");
    }

    /// <summary>
    /// Represents the Input Model.
    /// </summary>
    private sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the Email.
        /// </summary>
        [Required]
        [EmailAddress]
        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        public string Email { get; set; } = "";
    }
}
