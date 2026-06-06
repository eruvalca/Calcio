using System.ComponentModel.DataAnnotations;
using System.Text;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the Reset Password.
/// </summary>
/// <param name="redirectManager">The redirect Manager.</param>
/// <param name="userManager">The user Manager.</param>
public partial class ResetPassword(
    IdentityRedirectManager redirectManager,
    UserManager<CalcioUserEntity> userManager)
{
    /// <summary>
    /// Stores the identity Errors.
    /// </summary>
    private IEnumerable<IdentityError>? identityErrors;

    /// <summary>
    /// Gets or sets the Input.
    /// </summary>
    [SupplyParameterFromForm]
    /// <summary>
    /// Gets or sets the input.
    /// </summary>
    private InputModel Input { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Code.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the code.
    /// </summary>
    private string? Code { get; set; }

    /// <summary>
    /// Gets the Message.
    /// </summary>
    private string? Message => identityErrors is null ? null : $"Error: {string.Join(", ", identityErrors.Select(error => error.Description))}";

    /// <summary>
    /// Executes the On Initialized operation.
    /// </summary>
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

    /// <summary>
    /// Executes the On Valid Submit Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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

        /// <summary>
        /// Gets or sets the Password.
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; } = "";

        /// <summary>
        /// Gets or sets the Confirm Password.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        /// <summary>
        /// Gets or sets the confirm password.
        /// </summary>
        public string ConfirmPassword { get; set; } = "";

        /// <summary>
        /// Gets or sets the Code.
        /// </summary>
        [Required]
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        public string Code { get; set; } = "";
    }
}
