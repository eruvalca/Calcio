using System.Diagnostics;

using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Pages;

/// <summary>
/// Displays request correlation details when an unhandled server error occurs.
/// </summary>
public partial class Error
{
    /// <summary>
    /// Gets the cascading HTTP context for resolving the current request identifier.
    /// </summary>
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    /// <summary>
    /// Gets the current request identifier used for diagnostics.
    /// </summary>
    private string? RequestId { get; set; }

    /// <summary>
    /// Gets a value indicating whether a request identifier is available to display.
    /// </summary>
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>
    /// Initializes the page state with the current activity or HTTP trace identifier.
    /// </summary>
    protected override void OnInitialized()
        => RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
