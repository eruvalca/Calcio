using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

using Calcio.Data.Models.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Calcio.Components.Account.Pages;

public partial class Register(
    UserManager<CalcioUserEntity> userManager,
    IUserStore<CalcioUserEntity> userStore,
    SignInManager<CalcioUserEntity> signInManager,
    IEmailSender<CalcioUserEntity> emailSender,
    ILogger<Register> logger,
    NavigationManager navigationManager,
    IdentityRedirectManager redirectManager)
{
    private IEnumerable<IdentityError>? identityErrors;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    private string? Message
        => identityErrors is null
        ? null
        : $"Error: {string.Join(", ", identityErrors.Select(error => error.Description))}";

    protected override void OnInitialized() => Input ??= new();

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
            redirectManager.RedirectTo(
                "Account/RegisterConfirmation",
                new() { ["email"] = Input.Email, ["returnUrl"] = ReturnUrl });
        }
        else
        {
            await signInManager.SignInAsync(user, isPersistent: false);
            redirectManager.RedirectTo(ReturnUrl);
        }
    }

    private IUserEmailStore<CalcioUserEntity> GetEmailStore() => !userManager.SupportsUserEmail
        ? throw new NotSupportedException("The default UI requires a user store with email support.")
        : (IUserEmailStore<CalcioUserEntity>)userStore;

    private sealed class InputModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User created a new account with password.")]
    private static partial void LogUserCreatedAccountWithPassword(ILogger logger);
}
