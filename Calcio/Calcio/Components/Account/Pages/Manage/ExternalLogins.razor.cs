using Calcio.Data.Models.Entities;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

public partial class ExternalLogins(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IUserStore<CalcioUserEntity> userStore,
    IdentityRedirectManager redirectManager)
{
    public const string LinkLoginCallbackAction = "LinkLoginCallback";

    private CalcioUserEntity? user;
    private IList<UserLoginInfo>? currentLogins;
    private IList<AuthenticationScheme>? otherLogins;
    private bool showRemoveButton;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private string? LoginProvider { get; set; }

    [SupplyParameterFromForm]
    private string? ProviderKey { get; set; }

    [SupplyParameterFromQuery]
    private string? Action { get; set; }

    protected override async Task OnInitializedAsync()
    {
        user = await userManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        currentLogins = await userManager.GetLoginsAsync(user);
        otherLogins = [.. (await signInManager.GetExternalAuthenticationSchemesAsync()).Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider))];

        string? passwordHash = null;
        if (userStore is IUserPasswordStore<CalcioUserEntity> userPasswordStore)
        {
            passwordHash = await userPasswordStore.GetPasswordHashAsync(user, HttpContext.RequestAborted);
        }

        showRemoveButton = passwordHash is not null || currentLogins.Count > 1;

        if (HttpMethods.IsGet(HttpContext.Request.Method) && Action == LinkLoginCallbackAction)
        {
            await OnGetLinkLoginCallbackAsync();
        }
    }

    private async Task OnSubmitAsync()
    {
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        var result = await userManager.RemoveLoginAsync(user, LoginProvider!, ProviderKey!);
        if (!result.Succeeded)
        {
            redirectManager.RedirectToCurrentPageWithStatus("Error: The external login was not removed.", HttpContext);
        }
        else
        {
            await signInManager.RefreshSignInAsync(user);
            redirectManager.RedirectToCurrentPageWithStatus("The external login was removed.", HttpContext);
        }
    }

    private async Task OnGetLinkLoginCallbackAsync()
    {
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        var userId = await userManager.GetUserIdAsync(user);
        var info = await signInManager.GetExternalLoginInfoAsync(userId);
        if (info is null)
        {
            redirectManager.RedirectToCurrentPageWithStatus("Error: Could not load external login info.", HttpContext);
            return;
        }

        var result = await userManager.AddLoginAsync(user, info);
        if (result.Succeeded)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            redirectManager.RedirectToCurrentPageWithStatus("The external login was added.", HttpContext);
        }
        else
        {
            redirectManager.RedirectToCurrentPageWithStatus("Error: The external login was not added. External logins can only be associated with one account.", HttpContext);
        }
    }
}
