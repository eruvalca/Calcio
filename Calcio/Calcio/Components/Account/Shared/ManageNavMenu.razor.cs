using Calcio.Data.Models.Entities;

using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Shared;

public partial class ManageNavMenu(SignInManager<CalcioUserEntity> signInManager)
{
    private bool hasExternalLogins;

    protected override async Task OnInitializedAsync()
        => hasExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).Any();
}
