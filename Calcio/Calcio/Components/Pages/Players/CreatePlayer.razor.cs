using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Pages.Players;

public partial class CreatePlayer
{
    [Parameter]
    public long ClubId { get; set; }
}
