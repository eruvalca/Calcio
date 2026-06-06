using Calcio.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Calcio.Components.Account;

/// <summary>
/// Represents the Identity Redirect Manager.
/// </summary>
/// <param name="navigationManager">The navigation Manager.</param>
public sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    /// <summary>
    /// Stores the Status Cookie Name.
    /// </summary>
    public const string StatusCookieName = "Identity.StatusMessage";

    /// <summary>
    /// Stores the Status Cookie Builder.
    /// </summary>
    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    /// <summary>
    /// Executes the Redirect To operation.
    /// </summary>
    /// <param name="uri">The uri.</param>
    public void RedirectTo(string? uri)
    {
        uri ??= "";

        // Prevent open redirects.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        navigationManager.NavigateTo(uri);
    }

    /// <summary>
    /// Executes the Redirect To operation.
    /// </summary>
    /// <param name="uri">The uri.</param>
    /// <param name="queryParameters">The query Parameters.</param>
    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    /// <summary>
    /// Executes the Redirect To With Status operation.
    /// </summary>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message.</param>
    /// <param name="context">The context.</param>
    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    /// <summary>
    /// Gets the Current Path.
    /// </summary>
    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

    /// <summary>
    /// Executes the Redirect To Current Page operation.
    /// </summary>
    public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

    /// <summary>
    /// Executes the Redirect To Current Page With Status operation.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="context">The context.</param>
    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
        => RedirectToWithStatus(CurrentPath, message, context);

    /// <summary>
    /// Executes the Redirect To Invalid User operation.
    /// </summary>
    /// <param name="userManager">The user Manager.</param>
    /// <param name="context">The context.</param>
    public void RedirectToInvalidUser(UserManager<CalcioUserEntity> userManager, HttpContext context)
        => RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
}
