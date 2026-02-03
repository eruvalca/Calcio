using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Extensions.Shared;
using Calcio.Shared.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Players.Shared;

[Authorize(Roles = Roles.ClubAdmin)]
public partial class ClubPlayersGrid
{
    [Parameter]
    public required long ClubId { get; set; }

    [Parameter]
    public required List<ClubPlayerDto> Players { get; set; }

    [Parameter]
    public EventCallback OnPlayersChanged { get; set; }

    private string SearchTerm { get; set; } = string.Empty;
    private bool ShowImport { get; set; }

    private IEnumerable<ClubPlayerDto> FilteredPlayers
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Players
            : Players.Where(player => player.FullName.ContainsIgnoreCase(SearchTerm)
                || player.FirstName.ContainsIgnoreCase(SearchTerm)
                || player.LastName.ContainsIgnoreCase(SearchTerm));

    private void ShowImportModal()
    {
        ShowImport = true;
    }

    private void HideImportModal()
    {
        ShowImport = false;
    }

    private async Task HandleImportComplete()
    {
        // Notify parent to refresh players list
        await OnPlayersChanged.InvokeAsync();
    }
}
