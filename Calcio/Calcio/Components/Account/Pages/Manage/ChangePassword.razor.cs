using System.ComponentModel.DataAnnotations;

using Calcio.Data;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class ChangePassword(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IdentityRedirectManager redirectManager,
    ILogger<ChangePassword> logger)
{
    private string? message;
    private ApplicationUser? user;
    private bool hasPassword;

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

        hasPassword = await userManager.HasPasswordAsync(user);
        if (!hasPassword)
        {
            redirectManager.RedirectTo("Account/Manage/SetPassword");
        }
    }

    private async Task OnValidSubmitAsync()
    {
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        var changePasswordResult = await userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            message = $"Error: {string.Join(",", changePasswordResult.Errors.Select(error => error.Description))}";
            return;
        }

        await signInManager.RefreshSignInAsync(user);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("User changed their password successfully.");
        }

        redirectManager.RedirectToCurrentPageWithStatus("Your password has been changed", HttpContext);
    }

    private sealed class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
