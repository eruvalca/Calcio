using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account.Pages.Manage;

/// <summary>
/// Represents the Generate Recovery Codes.
/// </summary>
/// <param name="userManager">The user Manager.</param>
/// <param name="redirectManager">The redirect Manager.</param>
/// <param name="logger">The logger.</param>
public partial class GenerateRecoveryCodes(
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager,
    ILogger<GenerateRecoveryCodes> logger)
{
    /// <summary>
    /// Stores the user.
    /// </summary>
    private string? message;
    /// <summary>
    /// Stores the recovery Codes.
    /// </summary>
    private CalcioUserEntity? user;
    /// <summary>
    /// Stores the recovery Codes.
    /// </summary>
    private IEnumerable<string>? recoveryCodes;

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
        user = await userManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            redirectManager.RedirectToInvalidUser(userManager, HttpContext);
            return;
        }

        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        if (!isTwoFactorEnabled)
        {
            throw new InvalidOperationException("Cannot generate recovery codes for user because they do not have 2FA enabled.");
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

        var userId = await userManager.GetUserIdAsync(user);
        recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        message = "You have generated new recovery codes.";

        LogUserGeneratedRecoveryCodes(logger, userId);
    }

    /// <summary>
    /// Executes the Log User Generated Recovery Codes operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user Id.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "User with ID '{UserId}' has generated new 2FA recovery codes.")]
    /// <summary>
    /// Executes the log user generated recovery codes operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user id.</param>
    private static partial void LogUserGeneratedRecoveryCodes(ILogger logger, string userId);
}
