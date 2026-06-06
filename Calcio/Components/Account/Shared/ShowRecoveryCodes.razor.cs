using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Account.Shared;

/// <summary>
/// Represents the Show Recovery Codes.
/// </summary>
public partial class ShowRecoveryCodes
{
    /// <summary>
    /// Gets or sets the Recovery Codes.
    /// </summary>
    [Parameter]
    /// <summary>
    /// Gets or sets the recovery codes.
    /// </summary>
    public string[] RecoveryCodes { get; set; } = [];

    /// <summary>
    /// Gets or sets the Status Message.
    /// </summary>
    [Parameter]
    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string? StatusMessage { get; set; }
}
