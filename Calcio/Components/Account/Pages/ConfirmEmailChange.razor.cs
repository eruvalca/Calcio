using System.Text;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the Confirm Email Change.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class ConfirmEmailChange(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Stores the message.
    /// </summary>
    private string? message;

    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the User Id.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    private string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the Email.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    private string? Email { get; set; }

    /// <summary>
    /// Gets or sets the Code.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the code.
    /// </summary>
    private string? Code { get; set; }

    /// <summary>
    /// Executes the On Initialized Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    protected override async Task OnInitializedAsync()
    {
        if (UserId is null || Email is null || Code is null)
        {
            redirectManager.RedirectToWithStatus(
                "Account/Login", "Error: Invalid email change confirmation link.", HttpContext);
            return;
        }

        var user = await userManager.FindByIdAsync(UserId);
        if (user is null)
        {
            message = "Unable to find user with Id '{userId}'";
            return;
        }

        var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Code));
        var result = await userManager.ChangeEmailAsync(user, Email, code);
        if (!result.Succeeded)
        {
            message = "Error changing email.";
            return;
        }

        // In our UI email and user name are one and the same, so when we update the email
        // we need to update the user name.
        var setUserNameResult = await userManager.SetUserNameAsync(user, Email);
        if (!setUserNameResult.Succeeded)
        {
            message = "Error changing user name.";
            return;
        }

        await signInManager.RefreshSignInAsync(user);
        message = "Thank you for confirming your email change.";
    }
}
