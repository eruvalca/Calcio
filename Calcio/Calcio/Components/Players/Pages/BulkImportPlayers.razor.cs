using Microsoft.AspNetCore.Components;

namespace Calcio.Components.Players.Pages;

public partial class BulkImportPlayers
{
    [Parameter]
    public long ClubId { get; set; }
}
