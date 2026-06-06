using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the Two Factor Authentication.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class TwoFactorAuthentication(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Stores the has Authenticator.
    /// </summary>
    private bool canTrack;
    /// <summary>
    /// Stores the recovery Codes Left.
    /// </summary>
    private bool hasAuthenticator;
    /// <summary>
    /// Stores the is2fa Enabled.
    /// </summary>
    private int recoveryCodesLeft;
    /// <summary>
    /// Stores the is Machine Remembered.
    /// </summary>
    private bool is2faEnabled;
    /// <summary>
    /// Stores the is Machine Remembered.
    /// </summary>
    private bool isMachineRemembered;

    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Executes the On Initialized Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    protected override async Task OnInitializedAsync()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        canTrack = HttpContext.Features.Get<ITrackingConsentFeature>()?.CanTrack ?? true;
        hasAuthenticator = await userManager.GetAuthenticatorKeyAsync(user) is not null;
        is2faEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        isMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user);
        recoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user);
    }

    /// <summary>
    /// Executes the On Submit Forget Browser Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    private async Task OnSubmitForgetBrowserAsync()
    {
        await signInManager.ForgetTwoFactorClientAsync();

        redirectManager.RedirectToCurrentPageWithStatus(
            "The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.",
            HttpContext);
    }
}
