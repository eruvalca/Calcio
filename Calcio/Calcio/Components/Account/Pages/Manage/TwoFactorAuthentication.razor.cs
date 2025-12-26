using Calcio.Shared.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class TwoFactorAuthentication(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager)
{
    private bool canTrack;
    private bool hasAuthenticator;
    private int recoveryCodesLeft;
    private bool is2faEnabled;
    private bool isMachineRemembered;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

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

    private async Task OnSubmitForgetBrowserAsync()
    {
        await signInManager.ForgetTwoFactorClientAsync();

        redirectManager.RedirectToCurrentPageWithStatus(
            "The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.",
            HttpContext);
    }
}
