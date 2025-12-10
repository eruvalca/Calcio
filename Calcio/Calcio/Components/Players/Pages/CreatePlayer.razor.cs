using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Players.Pages;

public partial class CreatePlayer
{
    [Parameter]
    public long ClubId { get; set; }
}
