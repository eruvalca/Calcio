using Microsoft.AspNetCore.Identity;
using Calcio.Data;

namespace Calcio.Components.Account.Shared;

public partial class ManageNavMenu(SignInManager<ApplicationUser> signInManager)
{
    private bool hasExternalLogins;

    protected override async Task OnInitializedAsync()
    {
        hasExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).Any();
    }
}
