using System.ComponentModel.DataAnnotations;

using Calcio.Data.Contexts;
using Calcio.Entities;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the Login.
/// </summary>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="userManager">The user Manager.</param>
/// <param name="dbContextFactory">The db Context Factory.</param>
/// <param name="logger">The logger.</param>
/// <param name="navigationManager">The navigation Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class Login(
    SignInManager<CalcioUserEntity> signInManager,
    UserManager<CalcioUserEntity> userManager,
    IDbContextFactory<ReadOnlyDbContext> dbContextFactory,
    ILogger<Login> logger,
    NavigationManager navigationManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Stores the error Message.
    /// </summary>
    private string? errorMessage;
    /// <summary>
    /// Stores the edit Context.
    /// </summary>
    private EditContext editContext = default!;

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
    {
        Input ??= new();

        editContext = new EditContext(Input);

        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    /// <summary>
    /// Executes the Login User operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public async Task LoginUser()
    {
        if (!string.IsNullOrEmpty(Input.Passkey?.Error))
        {
            errorMessage = $"Error: {Input.Passkey.Error}";
            return;
        }

        SignInResult result;
        if (!string.IsNullOrEmpty(Input.Passkey?.CredentialJson))
        {
            // When performing passkey sign-in, don't perform form validation.
            result = await signInManager.PasskeySignInAsync(Input.Passkey.CredentialJson);
        }
        else
        {
            // If doing a password sign-in, validate the form.
            if (!editContext.Validate())
            {
                return;
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            result = await signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
        }

        if (result.Succeeded)
        {
            LogUserLoggedIn(logger);

            // Check if user has uploaded a profile photo
            var user = await userManager.FindByEmailAsync(Input.Email);
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
        }
        else if (result.RequiresTwoFactor)
        {
            redirectManager.RedirectTo(
                "Account/LoginWith2fa",
                new() { ["returnUrl"] = ReturnUrl, ["rememberMe"] = Input.RememberMe });
        }
        else if (result.IsLockedOut)
        {
            LogUserAccountLockedOut(logger);
            redirectManager.RedirectTo("Account/Lockout");
        }
        else
        {
            var user = await userManager.FindByEmailAsync(Input.Email);
            errorMessage = user is null
                ? "Error: No account found with that email address."
                : "Error: Invalid login attempt.";
        }
    }

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

        /// <summary>
        /// Gets or sets the Password.
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; } = "";

        /// <summary>
        /// Gets or sets the Remember Me.
        /// </summary>
        [Display(Name = "Remember me?")]
        /// <summary>
        /// Gets or sets the remember me.
        /// </summary>
        public bool RememberMe { get; set; }

        /// <summary>
        /// Gets or sets the Passkey.
        /// </summary>
        public PasskeyInputModel? Passkey { get; set; }
    }

    /// <summary>
    /// Executes the Log User Logged In operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User logged in.")]
    /// <summary>
    /// Executes the log user logged in operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    private static partial void LogUserLoggedIn(ILogger logger);

    /// <summary>
    /// Executes the Log User Account Locked Out operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "User account locked out.")]
    /// <summary>
    /// Executes the log user account locked out operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    private static partial void LogUserAccountLockedOut(ILogger logger);
}
