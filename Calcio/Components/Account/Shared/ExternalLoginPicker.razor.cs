using Calcio.Entities;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Shared;

/// <summary>
/// Represents the External Login Picker.
/// </summary>
/// <param name="signInManager">The sign In Manager.</param>
public partial class ExternalLoginPicker(
    SignInManager<CalcioUserEntity> signInManager)
{
    /// <summary>
    /// Stores the external Logins.
    /// </summary>
    private AuthenticationScheme[] externalLogins = [];

    /// <summary>
    /// Gets or sets the Return Url.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the return url.
    /// </summary>
    private string? ReturnUrl { get; set; }

    /// <summary>
    /// Executes the On Initialized Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    protected override async Task OnInitializedAsync()
        => externalLogins = [.. await signInManager.GetExternalAuthenticationSchemesAsync()];
}
