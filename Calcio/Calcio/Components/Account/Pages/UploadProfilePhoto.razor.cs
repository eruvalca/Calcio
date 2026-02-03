using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Account.Pages;

public partial class UploadProfilePhoto
{
    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }
}
