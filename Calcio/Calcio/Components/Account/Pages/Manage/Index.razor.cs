using System.ComponentModel.DataAnnotations;

using Calcio.Shared.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class Index(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager)
{
    private CalcioUserEntity? user;
    private string? username;
    private string? phoneNumber;

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

        username = await userManager.GetUserNameAsync(user);
        phoneNumber = await userManager.GetPhoneNumberAsync(user);

        Input.PhoneNumber ??= phoneNumber;
        Input.FirstName ??= user.FirstName;
        Input.LastName ??= user.LastName;
    }

    private async Task OnValidSubmitAsync()
    {
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        var hasUserChanges = false;

        if (Input.FirstName is not null && Input.FirstName != user.FirstName)
        {
            user.FirstName = Input.FirstName;
            hasUserChanges = true;
        }

        if (Input.LastName is not null && Input.LastName != user.LastName)
        {
            user.LastName = Input.LastName;
            hasUserChanges = true;
        }

        if (Input.PhoneNumber is not null && Input.PhoneNumber != phoneNumber)
        {
            var setPhoneResult = await userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            if (!setPhoneResult.Succeeded)
            {
                redirectManager.RedirectToCurrentPageWithStatus("Error: Failed to set phone number.", HttpContext);
                return;
            }

            phoneNumber = Input.PhoneNumber;
        }

        if (hasUserChanges)
        {
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                redirectManager.RedirectToCurrentPageWithStatus("Error: Failed to update profile.", HttpContext);
                return;
            }
        }

        await signInManager.RefreshSignInAsync(user);
        redirectManager.RedirectToCurrentPageWithStatus("Your profile has been updated", HttpContext);
    }

    private sealed class InputModel
    {
        [Required]
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [Required]
        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }
}
