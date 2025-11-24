using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

using Calcio.Shared.Models.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages;

public partial class ExternalLogin(
    SignInManager<CalcioUserEntity> signInManager,
    UserManager<CalcioUserEntity> userManager,
    IUserStore<CalcioUserEntity> userStore,
    IEmailSender<CalcioUserEntity> emailSender,
    NavigationManager navigationManager,
    IdentityRedirectManager redirectManager,
    ILogger<ExternalLogin> logger)
{
    public const string LoginCallbackAction = "LoginCallback";

    private string? message;
    private ExternalLoginInfo? externalLoginInfo;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? RemoteError { get; set; }

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery]
    private string? Action { get; set; }

    private string? ProviderDisplayName => externalLoginInfo?.ProviderDisplayName;

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        if (RemoteError is not null)
        {
            redirectManager.RedirectToWithStatus("Account/Login", $"Error from external provider: {RemoteError}", HttpContext);
            return;
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            redirectManager.RedirectToWithStatus("Account/Login", "Error loading external login information.", HttpContext);
            return;
        }

        externalLoginInfo = info;

        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            if (Action == LoginCallbackAction)
            {
                await OnLoginCallbackAsync();
                return;
            }

            // We should only reach this page via the login callback, so redirect back to
            // the login page if we get here some other way.
            redirectManager.RedirectTo("Account/Login");
        }
    }

    private async Task OnLoginCallbackAsync()
    {
        if (externalLoginInfo is null)
        {
            redirectManager.RedirectToWithStatus("Account/Login", "Error loading external login information.", HttpContext);
            return;
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await signInManager.ExternalLoginSignInAsync(
            externalLoginInfo.LoginProvider,
            externalLoginInfo.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (result.Succeeded)
        {
            LogUserLoggedInWithProvider(logger, externalLoginInfo.Principal.Identity?.Name, externalLoginInfo.LoginProvider);
            redirectManager.RedirectTo(ReturnUrl);
            return;
        }
        else if (result.IsLockedOut)
        {
            redirectManager.RedirectTo("Account/Lockout");
            return;
        }

        // If the user does not have an account, then ask the user to create an account.
        if (externalLoginInfo.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
        {
            Input.Email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
        }
    }

    private async Task OnValidSubmitAsync()
    {
        if (externalLoginInfo is null)
        {
            redirectManager.RedirectToWithStatus("Account/Login", "Error loading external login information during confirmation.", HttpContext);
            return;
        }

        var emailStore = GetEmailStore();
        var user = CreateUser();

        await userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        await emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

        var result = await userManager.CreateAsync(user);
        if (result.Succeeded)
        {
            result = await userManager.AddLoginAsync(user, externalLoginInfo);
            if (result.Succeeded)
            {
                LogUserCreatedAccountWithProvider(logger, externalLoginInfo.LoginProvider);

                var userId = await userManager.GetUserIdAsync(user);
                var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = navigationManager.GetUriWithQueryParameters(
                    navigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri,
                    new Dictionary<string, object?> { ["userId"] = userId, ["code"] = code });
                await emailSender.SendConfirmationLinkAsync(user, Input.Email, HtmlEncoder.Default.Encode(callbackUrl));

                // If account confirmation is required, we need to show the link if we don't have a real email sender
                if (userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    redirectManager.RedirectTo("Account/RegisterConfirmation", new() { ["email"] = Input.Email });
                }
                else
                {
                    await signInManager.SignInAsync(user, isPersistent: false, externalLoginInfo.LoginProvider);
                    redirectManager.RedirectTo(ReturnUrl);
                }
            }
        }
        else
        {
            message = $"Error: {string.Join(",", result.Errors.Select(error => error.Description))}";
        }
    }

    private CalcioUserEntity CreateUser()
    {
        try
        {
            return Activator.CreateInstance<CalcioUserEntity>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(CalcioUserEntity)}'. " +
                $"Ensure that '{nameof(CalcioUserEntity)}' is not an abstract class and has a parameterless constructor");
        }
    }

    private IUserEmailStore<CalcioUserEntity> GetEmailStore() => !userManager.SupportsUserEmail
        ? throw new NotSupportedException("The default UI requires a user store with email support.")
        : (IUserEmailStore<CalcioUserEntity>)userStore;

    private sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Name} logged in with {LoginProvider} provider.")]
    private static partial void LogUserLoggedInWithProvider(ILogger logger, string? name, string loginProvider);

    [LoggerMessage(Level = LogLevel.Information, Message = "User created an account using {Name} provider.")]
    private static partial void LogUserCreatedAccountWithProvider(ILogger logger, string name);
}
