using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Account.Shared;

/// <summary>
/// Represents the Status Message.
/// </summary>
public partial class StatusMessage
{
    /// <summary>
    /// Stores the message From Cookie.
    /// </summary>
    private string? messageFromCookie;

    /// <summary>
    /// Gets or sets the Message.
    /// </summary>
    [Parameter]
    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the Http Context.
    /// </summary>
    [CascadingParameter]
    /// <summary>
    /// Gets or sets the http context.
    /// </summary>
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets the Display Message.
    /// </summary>
    private string? DisplayMessage => Message ?? messageFromCookie;

    /// <summary>
    /// Executes the On Initialized operation.
    /// </summary>
    protected override void OnInitialized()
    {
        messageFromCookie = HttpContext.Request.Cookies[IdentityRedirectManager.StatusCookieName];

        if (messageFromCookie is not null)
        {
            HttpContext.Response.Cookies.Delete(IdentityRedirectManager.StatusCookieName);
        }
    }
}
