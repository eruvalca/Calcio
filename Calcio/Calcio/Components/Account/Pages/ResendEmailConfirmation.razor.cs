using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

using Calcio.Shared.Models.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages;

public partial class ResendEmailConfirmation(
    UserManager<CalcioUserEntity> userManager,
    IEmailSender<CalcioUserEntity> emailSender,
    NavigationManager navigationManager)
{
    private string? message;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    protected override void OnInitialized() => Input ??= new();

    private async Task OnValidSubmitAsync()
    {
        var user = await userManager.FindByEmailAsync(Input.Email!);
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

    private sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}
