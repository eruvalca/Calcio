using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the Email.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="emailSender">The email Sender.</param>
/// <param name="navigationManager">The navigation Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class Email(
    UserManager<CalcioUserEntity> userManager,
    IEmailSender<CalcioUserEntity> emailSender,
    NavigationManager navigationManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Stores the user.
    /// </summary>
    private string? message;
    /// <summary>
    /// Stores the email.
    /// </summary>
    private CalcioUserEntity? user;
    /// <summary>
    /// Stores the is Email Confirmed.
    /// </summary>
    private string? email;
    /// <summary>
    /// Stores the is Email Confirmed.
    /// </summary>
    private bool isEmailConfirmed;

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
    [SupplyParameterFromForm(FormName = "change-email")]
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

        email = await userManager.GetEmailAsync(user);
        isEmailConfirmed = await userManager.IsEmailConfirmedAsync(user);

        Input.NewEmail ??= email;
    }

    /// <summary>
    /// Executes the On Valid Submit Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task OnValidSubmitAsync()
    {
        if (Input.NewEmail is null || Input.NewEmail == email)
        {
            message = "Your email is unchanged.";
            return;
        }

        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        var userId = await userManager.GetUserIdAsync(user);
        var code = await userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = navigationManager.GetUriWithQueryParameters(
            navigationManager.ToAbsoluteUri("Account/ConfirmEmailChange").AbsoluteUri,
            new Dictionary<string, object?> { ["userId"] = userId, ["email"] = Input.NewEmail, ["code"] = code });

        await emailSender.SendConfirmationLinkAsync(user, Input.NewEmail, HtmlEncoder.Default.Encode(callbackUrl));

        message = "Confirmation link to change email sent. Please check your email.";
    }

    /// <summary>
    /// Executes the On Send Email Verification Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task OnSendEmailVerificationAsync()
    {
        if (email is null)
        {
            return;
        }

        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        var userId = await userManager.GetUserIdAsync(user);
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = navigationManager.GetUriWithQueryParameters(
            navigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri,
            new Dictionary<string, object?> { ["userId"] = userId, ["code"] = code });

        await emailSender.SendConfirmationLinkAsync(user, email, HtmlEncoder.Default.Encode(callbackUrl));

        message = "Verification email sent. Please check your email.";
    }

    /// <summary>
    /// Represents the Input Model.
    /// </summary>
    private sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the New Email.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "New email")]
        /// <summary>
        /// Gets or sets the new email.
        /// </summary>
        public string? NewEmail { get; set; }
    }
}
