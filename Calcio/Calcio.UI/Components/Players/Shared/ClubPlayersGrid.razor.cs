using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Extensions.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Players.Shared;

[Authorize(Roles = "ClubAdmin")]
public partial class ClubPlayersGrid
{
    [Parameter]
    public required List<ClubPlayerDto> Players { get; set; }

    private string SearchTerm { get; set; } = string.Empty;

    private IEnumerable<ClubPlayerDto> FilteredPlayers
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Players
            : Players.Where(player => player.FullName.ContainsIgnoreCase(SearchTerm)
                || player.FirstName.ContainsIgnoreCase(SearchTerm)
                || player.LastName.ContainsIgnoreCase(SearchTerm));
}
