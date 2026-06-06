using Calcio.Entities;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the External Logins.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="userStore">The user Store.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class ExternalLogins(
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IUserStore<CalcioUserEntity> userStore,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Stores the Link Login Callback Action.
    /// </summary>
    public const string LinkLoginCallbackAction = "LinkLoginCallback";

    /// <summary>
    /// Stores the current Logins.
    /// </summary>
    private CalcioUserEntity? user;
    /// <summary>
    /// Stores the other Logins.
    /// </summary>
    private IList<UserLoginInfo>? currentLogins;
    /// <summary>
    /// Stores the show Remove Button.
    /// </summary>
    private IList<AuthenticationScheme>? otherLogins;
    /// <summary>
    /// Stores the show Remove Button.
    /// </summary>
    private bool showRemoveButton;

    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Login Provider.
    /// </summary>
    [SupplyParameterFromForm]
    /// <summary>
    /// Gets or sets the login provider.
    /// </summary>
    private string? LoginProvider { get; set; }

    /// <summary>
    /// Gets or sets the Provider Key.
    /// </summary>
    [SupplyParameterFromForm]
    /// <summary>
    /// Gets or sets the provider key.
    /// </summary>
    private string? ProviderKey { get; set; }

    /// <summary>
    /// Gets or sets the Action.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    private string? Action { get; set; }

    /// <summary>
    /// Executes the On Initialized Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the On Submit Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the On Get Link Login Callback Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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
