using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the Register.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="userStore">The user Store.</param>
/// <param name="signInManager">The sign In Manager.</param>
/// <param name="emailSender">The email Sender.</param>
/// <param name="logger">The logger.</param>
/// <param name="navigationManager">The navigation Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
public partial class Register(
    UserManager<CalcioUserEntity> userManager,
    IUserStore<CalcioUserEntity> userStore,
    SignInManager<CalcioUserEntity> signInManager,
    IEmailSender<CalcioUserEntity> emailSender,
    ILogger<Register> logger,
    NavigationManager navigationManager,
    IdentityRedirectManager redirectManager)
{
    /// <summary>
    /// Stores the identity Errors.
    /// </summary>
    private IEnumerable<IdentityError>? identityErrors;

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
    /// Gets the Message.
    /// </summary>
    private string? Message
        => identityErrors is null
        ? null
        : $"Error: {string.Join(", ", identityErrors.Select(error => error.Description))}";

    /// <summary>
    /// Executes the On Initialized operation.
    /// </summary>
    protected override void OnInitialized() => Input ??= new();

    /// <summary>
    /// Executes the Register User operation.
    /// </summary>
    /// <param name="editContext">The edit Context.</param>
    /// <returns>The operation result.</returns>
    public async Task RegisterUser(EditContext editContext)
    {
        var user = new CalcioUserEntity()
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName
        };

        await userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        var emailStore = GetEmailStore();
        await emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
        var result = await userManager.CreateAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            identityErrors = result.Errors;
            return;
        }

        LogUserCreatedAccountWithPassword(logger);

        var userId = await userManager.GetUserIdAsync(user);
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = navigationManager.GetUriWithQueryParameters(
            navigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri,
            new Dictionary<string, object?> { ["userId"] = userId, ["code"] = code, ["returnUrl"] = ReturnUrl });

        await emailSender.SendConfirmationLinkAsync(user, Input.Email, HtmlEncoder.Default.Encode(callbackUrl));

        if (userManager.Options.SignIn.RequireConfirmedAccount)
        {
            // Redirect to photo upload first, then to registration confirmation
            redirectManager.RedirectTo(
                "Account/UploadProfilePhoto",
                new()
                {
                    ["returnUrl"] = navigationManager.GetUriWithQueryParameters(
                        "Account/RegisterConfirmation",
                        new Dictionary<string, object?> { ["email"] = Input.Email, ["returnUrl"] = ReturnUrl })
                });
        }
        else
        {
            await signInManager.SignInAsync(user, isPersistent: false);
            // Redirect to photo upload first, then to intended destination
            redirectManager.RedirectTo(
                "Account/UploadProfilePhoto",
                new() { ["returnUrl"] = ReturnUrl });
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
        /// Gets or sets the First Name.
        /// </summary>
        [Required]
        [Display(Name = "First Name")]
        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string FirstName { get; set; } = "";

        /// <summary>
        /// Gets or sets the Last Name.
        /// </summary>
        [Required]
        [Display(Name = "Last Name")]
        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string LastName { get; set; } = "";

        /// <summary>
        /// Gets or sets the Email.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        public string Email { get; set; } = "";

        /// <summary>
        /// Gets or sets the Password.
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; } = "";

        /// <summary>
        /// Gets or sets the Confirm Password.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        /// <summary>
        /// Gets or sets the confirm password.
        /// </summary>
        public string ConfirmPassword { get; set; } = "";
    }

    /// <summary>
    /// Executes the Log User Created Account With Password operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User created a new account with password.")]
    /// <summary>
    /// Executes the log user created account with password operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    private static partial void LogUserCreatedAccountWithPassword(ILogger logger);
}
