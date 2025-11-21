using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;

using Calcio.Data.Models.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class RenamePasskey(
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager)
{
    private CalcioUserEntity? user;
    private UserPasskeyInfo? passkey;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [Parameter]
    public string? Id { get; set; }

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        user = (await userManager.GetUserAsync(HttpContext.User))!;
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

    private sealed class InputModel
    {
        [Required]
        [StringLength(200, ErrorMessage = "Passkey names must be no longer than {1} characters.")]
        public string Name { get; set; } = "";
    }
}
