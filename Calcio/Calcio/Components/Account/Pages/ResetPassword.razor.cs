using System.ComponentModel.DataAnnotations;
using System.Text;

using Calcio.Shared.Models.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages;

public partial class ResetPassword(
    IdentityRedirectManager redirectManager,
    UserManager<CalcioUserEntity> userManager)
{
    private IEnumerable<IdentityError>? identityErrors;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? Code { get; set; }

    private string? Message => identityErrors is null ? null : $"Error: {string.Join(", ", identityErrors.Select(error => error.Description))}";

    protected override void OnInitialized()
    {
        Input ??= new();

        if (Code is null)
        {
            redirectManager.RedirectTo("Account/InvalidPasswordReset");
            return;
        }

        Input.Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Code));
    }

    private async Task OnValidSubmitAsync()
    {
        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            // Don't reveal that the user does not exist
            redirectManager.RedirectTo("Account/ResetPasswordConfirmation");
            return;
        }

        var result = await userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
        if (result.Succeeded)
        {
            redirectManager.RedirectTo("Account/ResetPasswordConfirmation");
            return;
        }

        identityErrors = result.Errors;
    }

    private sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Required]
        public string Code { get; set; } = "";
    }
}
