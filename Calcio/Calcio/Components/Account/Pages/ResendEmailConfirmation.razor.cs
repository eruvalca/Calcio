using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the Resend Email Confirmation.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="emailSender">The email Sender.</param>
/// <param name="navigationManager">The navigation Manager.</param>
public partial class ResendEmailConfirmation(
    UserManager<CalcioUserEntity> userManager,
    IEmailSender<CalcioUserEntity> emailSender,
    NavigationManager navigationManager)
{
    /// <summary>
    /// Stores the message.
    /// </summary>
    private string? message;

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
        if (user is null)
        {
            message = "Verification email sent. Please check your email.";
            return;
        }

        var userId = await userManager.GetUserIdAsync(user);
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = navigationManager.GetUriWithQueryParameters(
            navigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri,
            new Dictionary<string, object?> { ["userId"] = userId, ["code"] = code });
        await emailSender.SendConfirmationLinkAsync(user, Input.Email, HtmlEncoder.Default.Encode(callbackUrl));

        message = "Verification email sent. Please check your email.";
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
