using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the Rename Passkey.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class RenamePasskey(
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Stores the passkey.
    /// </summary>
    private CalcioUserEntity? user;
    /// <summary>
    /// Stores the passkey.
    /// </summary>
    private UserPasskeyInfo? passkey;

    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Id.
    /// </summary>
    [Parameter]
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public string? Id { get; set; }

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

        byte[] credentialId;
        try
        {
            credentialId = Base64Url.DecodeFromChars(Id);
        }
        catch (FormatException)
        {
            redirectManager.RedirectToWithStatus("Account/Manage/Passkeys", "Error: The specified passkey ID had an invalid format.", HttpContext);
            return;
        }

        passkey = await userManager.GetPasskeyAsync(user, credentialId);
        if (passkey is null)
        {
            redirectManager.RedirectToWithStatus("Account/Manage/Passkeys", "Error: The specified passkey could not be found.", HttpContext);
            return;
        }
    }

    /// <summary>
    /// Executes the Rename operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task Rename()
    {
        passkey!.Name = Input.Name;
        var result = await userManager.AddOrUpdatePasskeyAsync(user!, passkey);
        if (!result.Succeeded)
        {
            redirectManager.RedirectToWithStatus("Account/Manage/Passkeys", "Error: The passkey could not be updated.", HttpContext);
            return;
        }

        redirectManager.RedirectToWithStatus("Account/Manage/Passkeys", "Passkey updated successfully.", HttpContext);
    }

    /// <summary>
    /// Represents the Input Model.
    /// </summary>
    private sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        [Required]
        [StringLength(200, ErrorMessage = "Passkey names must be no longer than {1} characters.")]
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = "";
    }
}
