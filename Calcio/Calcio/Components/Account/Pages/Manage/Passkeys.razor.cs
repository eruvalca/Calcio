using System.Buffers.Text;

using Calcio.Data;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class Passkeys(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IdentityRedirectManager redirectManager)
{
    private const int MaxPasskeyCount = 100;

    private ApplicationUser? user;
    private IList<UserPasskeyInfo>? currentPasskeys;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private string? Action { get; set; }

    [SupplyParameterFromForm]
    private string? CredentialId { get; set; }

    [SupplyParameterFromForm(FormName = "add-passkey")]
    private PasskeyInputModel Input { get; set; } = default!;

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
