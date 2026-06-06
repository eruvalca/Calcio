using System.ComponentModel.DataAnnotations;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the Index.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class Index(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Stores the username.
    /// </summary>
    private CalcioUserEntity? user;
    /// <summary>
    /// Stores the phone Number.
    /// </summary>
    private string? username;
    /// <summary>
    /// Stores the phone Number.
    /// </summary>
    private string? phoneNumber;

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
    [SupplyParameterFromForm]
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

        username = await userManager.GetUserNameAsync(user);
        phoneNumber = await userManager.GetPhoneNumberAsync(user);

        Input.PhoneNumber ??= phoneNumber;
        Input.FirstName ??= user.FirstName;
        Input.LastName ??= user.LastName;
    }

    /// <summary>
    /// Executes the On Valid Submit Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Represents the Input Model.
    /// </summary>
    private sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the First Name.
        /// </summary>
        [Required]
        [Display(Name = "First name")]
        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the Last Name.
        /// </summary>
        [Required]
        [Display(Name = "Last name")]
        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the Phone Number.
        /// </summary>
        [Phone]
        [Display(Name = "Phone number")]
        /// <summary>
        /// Gets or sets the phone number.
        /// </summary>
        public string? PhoneNumber { get; set; }
    }
}
