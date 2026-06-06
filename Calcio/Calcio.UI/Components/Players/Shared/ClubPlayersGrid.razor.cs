using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Extensions.Shared;
using Calcio.Shared.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Players.Shared;

[Authorize(Roles = Roles.ClubAdmin)]
/// <summary>
/// Displays club players and provides client-side filtering.
/// </summary>
public partial class ClubPlayersGrid
{
    /// <summary>
    /// Gets or sets the players displayed in the grid.
    /// </summary>
    [Parameter]
    public required List<ClubPlayerDto> Players { get; set; }

    /// <summary>
    /// Gets or sets the current search term used to filter players.
    /// </summary>
    private string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Gets players matching the current search term.
    /// </summary>
    private IEnumerable<ClubPlayerDto> FilteredPlayers
        => string.IsNullOrWhiteSpace(SearchTerm)
            ? Players
            : Players.Where(player => player.FullName.ContainsIgnoreCase(SearchTerm)
                || player.FirstName.ContainsIgnoreCase(SearchTerm)
                || player.LastName.ContainsIgnoreCase(SearchTerm));
}
