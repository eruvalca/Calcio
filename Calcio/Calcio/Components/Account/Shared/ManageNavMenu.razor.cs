using Calcio.Entities;

using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Shared;

/// <summary>
/// Represents the Manage Nav Menu.
/// </summary>
/// <param name="signInManager">The sign In Manager.</param>
public partial class ManageNavMenu(SignInManager<CalcioUserEntity> signInManager)
{
    /// <summary>
    /// Stores the has External Logins.
    /// </summary>
    private bool hasExternalLogins;

    /// <summary>
    /// Executes the On Initialized Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    protected override async Task OnInitializedAsync()
        => hasExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).Any();
}
