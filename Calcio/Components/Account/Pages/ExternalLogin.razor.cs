using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

using Calcio.Data.Contexts;
using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the External Login.
/// </summary>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="userManager">The user Manager.</param>
/// <param name="userStore">The user Store.</param>
/// <param name="emailSender">The email Sender.</param>
/// <param name="dbContextFactory">The db Context Factory.</param>
/// <param name="navigationManager">The navigation Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
/// <param name="logger">The logger.</param>
public partial class ExternalLogin(
    SignInManager<CalcioUserEntity> signInManager,
    UserManager<CalcioUserEntity> userManager,
    IUserStore<CalcioUserEntity> userStore,
    IEmailSender<CalcioUserEntity> emailSender,
    IDbContextFactory<ReadOnlyDbContext> dbContextFactory,
    NavigationManager navigationManager,
    IdentityRedirectManager redirectManager,
    ILogger<ExternalLogin> logger)
{
    /// <summary>
    /// Stores the Login Callback Action.
    /// </summary>
    public const string LoginCallbackAction = "LoginCallback";

    /// <summary>
    /// Stores the external Login Info.
    /// </summary>
    private string? message;
    /// <summary>
    /// Stores the external Login Info.
    /// </summary>
    private ExternalLoginInfo? externalLoginInfo;

    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Input.
    /// </summary>
    [SupplyParameterFromForm]
    /// <summary>
    /// Gets or sets the input.
    /// </summary>
    private InputModel Input { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Remote Error.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the remote error.
    /// </summary>
    private string? RemoteError { get; set; }

    /// <summary>
    /// Gets or sets the Return Url.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the return url.
    /// </summary>
    private string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the Action.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    private string? Action { get; set; }

    /// <summary>
    /// Gets the Provider Display Name.
    /// </summary>
    private string? ProviderDisplayName => externalLoginInfo?.ProviderDisplayName;

    /// <summary>
    /// Executes the On Initialized Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the On Login Callback Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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

            // Check if user has uploaded a profile photo
            var user = await userManager.FindByLoginAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey);
            if (user is not null)
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync();
                var hasPhoto = await dbContext.CalcioUserPhotos
                    .AnyAsync(p => p.CalcioUserId == user.Id);

                if (!hasPhoto)
                {
                    // Redirect to photo upload page, then to intended destination
                    redirectManager.RedirectTo(
                        "Account/UploadProfilePhoto",
                        new() { ["returnUrl"] = ReturnUrl });
                    return;
                }
            }

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

    /// <summary>
    /// Executes the On Valid Submit Async operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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
                    // Redirect to photo upload first, then to registration confirmation
                    redirectManager.RedirectTo(
                        "Account/UploadProfilePhoto",
                        new()
                        {
                            ["returnUrl"] = navigationManager.GetUriWithQueryParameters(
                                "Account/RegisterConfirmation",
                                new Dictionary<string, object?> { ["email"] = Input.Email })
                        });
                }
                else
                {
                    await signInManager.SignInAsync(user, isPersistent: false, externalLoginInfo.LoginProvider);
                    // Redirect to photo upload first, then to intended destination
                    redirectManager.RedirectTo(
                        "Account/UploadProfilePhoto",
                        new() { ["returnUrl"] = ReturnUrl });
                }
            }
        }
        else
        {
            message = $"Error: {string.Join(",", result.Errors.Select(error => error.Description))}";
        }
    }

    /// <summary>
    /// Executes the Create User operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Stores the user Store.
    /// </summary>
    /// <returns>The user email store.</returns>
    private IUserEmailStore<CalcioUserEntity> GetEmailStore() => !userManager.SupportsUserEmail
        ? throw new NotSupportedException("The default UI requires a user store with email support.")
        : (IUserEmailStore<CalcioUserEntity>)userStore;

    /// <summary>
    /// Represents the Input Model.
    /// </summary>
    private sealed class InputModel
    {
        /// <summary>
        /// Gets or sets the Email.
        /// </summary>
        [Required]
        [EmailAddress]
        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        public string Email { get; set; } = "";
    }

    /// <summary>
    /// Executes the Log User Logged In With Provider operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The name.</param>
    /// <param name="loginProvider">The login Provider.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "{Name} logged in with {LoginProvider} provider.")]
    /// <summary>
    /// Executes the log user logged in with provider operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The name.</param>
    /// <param name="loginProvider">The login provider.</param>
    private static partial void LogUserLoggedInWithProvider(ILogger logger, string? name, string loginProvider);

    /// <summary>
    /// Executes the Log User Created Account With Provider operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The name.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User created an account using {Name} provider.")]
    /// <summary>
    /// Executes the log user created account with provider operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The name.</param>
    private static partial void LogUserCreatedAccountWithProvider(ILogger logger, string name);
}
