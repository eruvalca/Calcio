using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Account.Shared;

public partial class ShowRecoveryCodes
{
    [Parameter]
    public string[] RecoveryCodes { get; set; } = [];

    [Parameter]
    public string? StatusMessage { get; set; }
}
