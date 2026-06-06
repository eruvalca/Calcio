using System.Buffers.Text;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the Passkeys.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class Passkeys(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Stores the Max Passkey Count.
    /// </summary>
    private const int MaxPasskeyCount = 100;

    /// <summary>
    /// Stores the current Passkeys.
    /// </summary>
    private CalcioUserEntity? user;
    /// <summary>
    /// Stores the current Passkeys.
    /// </summary>
    private IList<UserPasskeyInfo>? currentPasskeys;

    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Action.
    /// </summary>
    [SupplyParameterFromForm]
    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    private string? Action { get; set; }

    /// <summary>
    /// Gets or sets the Credential Id.
    /// </summary>
    [SupplyParameterFromForm]
    /// <summary>
    /// Gets or sets the credential id.
    /// </summary>
    private string? CredentialId { get; set; }

    /// <summary>
    /// Gets or sets the Input.
    /// </summary>
    [SupplyParameterFromForm(FormName = "add-passkey")]
    /// <summary>
    /// Gets or sets the input.
    /// </summary>
    private PasskeyInputModel Input { get; set; } = default!;

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

        currentPasskeys = await userManager.GetPasskeysAsync(user);
    }

    /// <summary>
    /// Executes the Add Passkey operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task AddPasskey()
    {
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        if (!string.IsNullOrEmpty(Input.Error))
        {
            redirectManager.RedirectToCurrentPageWithStatus($"Error: {Input.Error}", HttpContext);
            return;
        }

        if (string.IsNullOrEmpty(Input.CredentialJson))
        {
            redirectManager.RedirectToCurrentPageWithStatus("Error: The browser did not provide a passkey.", HttpContext);
            return;
        }

        if (currentPasskeys!.Count >= MaxPasskeyCount)
        {
            redirectManager.RedirectToCurrentPageWithStatus($"Error: You have reached the maximum number of allowed passkeys.", HttpContext);
            return;
        }

        var attestationResult = await signInManager.PerformPasskeyAttestationAsync(Input.CredentialJson);
        if (!attestationResult.Succeeded)
        {
            redirectManager.RedirectToCurrentPageWithStatus($"Error: Could not add the passkey: {attestationResult.Failure.Message}", HttpContext);
            return;
        }

        var addPasskeyResult = await userManager.AddOrUpdatePasskeyAsync(user, attestationResult.Passkey);
        if (!addPasskeyResult.Succeeded)
        {
            redirectManager.RedirectToCurrentPageWithStatus("Error: The passkey could not be added to your account.", HttpContext);
            return;
        }

        // Immediately prompt the user to enter a name for the credential
        var credentialIdBase64Url = Base64Url.EncodeToString(attestationResult.Passkey.CredentialId);
        redirectManager.RedirectTo($"Account/Manage/RenamePasskey/{credentialIdBase64Url}");
    }

    /// <summary>
    /// Executes the Update Passkey operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task UpdatePasskey()
    {
        switch (Action)
        {
            case "rename":
                redirectManager.RedirectTo($"Account/Manage/RenamePasskey/{CredentialId}");
                break;
            case "delete":
                await DeletePasskey();
                break;
            default:
                redirectManager.RedirectToCurrentPageWithStatus($"Error: Unknown action '{Action}'.", HttpContext);
                break;
        }
    }

    /// <summary>
    /// Executes the Delete Passkey operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task DeletePasskey()
    {
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        byte[] credentialId;
        try
        {
            credentialId = Base64Url.DecodeFromChars(CredentialId);
        }
        catch (FormatException)
        {
            redirectManager.RedirectToCurrentPageWithStatus("Error: The specified passkey ID had an invalid format.", HttpContext);
            return;
        }

        var result = await userManager.RemovePasskeyAsync(user, credentialId);
        if (!result.Succeeded)
        {
            redirectManager.RedirectToCurrentPageWithStatus("Error: The passkey could not be deleted.", HttpContext);
            return;
        }

        redirectManager.RedirectToCurrentPageWithStatus("Passkey deleted successfully.", HttpContext);
    }
}
