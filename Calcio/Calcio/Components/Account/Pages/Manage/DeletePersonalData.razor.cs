using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class DeletePersonalData(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager,
    ILogger<ChangePassword> logger)
{
    private string? message;
    private CalcioUserEntity? user;
    private bool requirePassword;

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

        requirePassword = await userManager.HasPasswordAsync(user);
    }

    private async Task OnValidSubmitAsync()
    {
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        if (requirePassword && !await userManager.CheckPasswordAsync(user, Input.Password))
        {
            message = "Error: Incorrect password.";
            return;
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Unexpected error occurred deleting user.");
        }

        await signInManager.SignOutAsync();

        var userId = await userManager.GetUserIdAsync(user);
        LogUserDeletedThemselves(logger, userId);

        redirectManager.RedirectToCurrentPage();
    }

    private sealed class InputModel
    {
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' deleted themselves.")]
    private static partial void LogUserDeletedThemselves(ILogger logger, string userId);
}
