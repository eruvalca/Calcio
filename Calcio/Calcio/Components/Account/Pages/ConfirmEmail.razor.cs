using System.Text;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the Confirm Email.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class ConfirmEmail(
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Stores the status Message.
    /// </summary>
    private string? statusMessage;

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
        if (UserId is null || Code is null)
        {
            redirectManager.RedirectTo("");
            return;
        }

        var user = await userManager.FindByIdAsync(UserId);
        if (user is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            statusMessage = $"Error loading user with ID {UserId}";
        }
        else
        {
            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Code));
            var result = await userManager.ConfirmEmailAsync(user, code);
            statusMessage = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";
        }
    }
}
