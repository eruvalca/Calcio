using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Calcio.Data;

namespace Calcio.Components.Account.Shared;

public partial class ExternalLoginPicker(
    SignInManager<ApplicationUser> signInManager)
{
    private AuthenticationScheme[] externalLogins = [];

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        externalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToArray();
    }
}
