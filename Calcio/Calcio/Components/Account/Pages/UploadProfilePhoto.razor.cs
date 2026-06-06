using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Account.Pages;

/// <summary>
/// Represents the Upload Profile Photo.
/// </summary>
public partial class UploadProfilePhoto
{
    /// <summary>
    /// Gets or sets the Return Url.
    /// </summary>
    [SupplyParameterFromQuery]
    /// <summary>
    /// Gets or sets the return url.
    /// </summary>
    private string? ReturnUrl { get; set; }
}
